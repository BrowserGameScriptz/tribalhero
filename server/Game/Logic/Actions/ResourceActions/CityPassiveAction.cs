#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle;
using Game.Data;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;

#endregion

namespace Game.Logic.Actions
{
    public class CityPassiveAction : ScheduledPassiveAction
    {
        private const int INTERVAL_IN_SECONDS = 1800;

        private readonly IActionFactory actionFactory;

        private readonly Procedure procedure;

        private readonly IGameObjectLocator locator;

        private uint cityId;

        private readonly Formula formula;

        private readonly ILocker locker;

        private readonly IObjectTypeFactory objectTypeFactory;

        private int laborTimeRemains;

        private readonly IBattleFormulas battleFormulas;

        private readonly IStructureCsvFactory structureFactory;

        private readonly TechnologyFactory technologyFactory;

        private readonly UnitFactory unitFactory;

        public CityPassiveAction(IObjectTypeFactory objectTypeFactory,
                                 ILocker locker,
                                 Formula formula,
                                 IActionFactory actionFactory,
                                 Procedure procedure,
                                 IGameObjectLocator locator,
                                 IBattleFormulas battleFormulas, 
                                 IStructureCsvFactory structureFactory, 
                                 TechnologyFactory technologyFactory, 
                                 UnitFactory unitFactory)
        {
            this.objectTypeFactory = objectTypeFactory;
            this.locker = locker;
            this.formula = formula;
            this.actionFactory = actionFactory;
            this.procedure = procedure;
            this.locator = locator;
            this.battleFormulas = battleFormulas;
            this.structureFactory = structureFactory;
            this.technologyFactory = technologyFactory;
            this.unitFactory = unitFactory;

            CreateSubscriptions();
        }

        public CityPassiveAction(uint cityId,
                                 IObjectTypeFactory objectTypeFactory,
                                 ILocker locker,
                                 Formula formula,
                                 IActionFactory actionFactory,
                                 Procedure procedure,
                                 IGameObjectLocator locator,
                                 IBattleFormulas battleFormulas, 
                                 IStructureCsvFactory structureFactory, 
                                 TechnologyFactory technologyFactory, 
                                 UnitFactory unitFactory)
            : this(objectTypeFactory, locker, formula, actionFactory, procedure, locator, battleFormulas, structureFactory, technologyFactory, unitFactory)
        {
            this.cityId = cityId;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
            cityId = uint.Parse(properties["city_id"]);
            laborTimeRemains = int.Parse(properties["labor_time_remains"]);
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.CityPassive;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                        {
                                new XmlKvPair("city_id", cityId), 
                                new XmlKvPair("labor_time_remains", laborTimeRemains)
                        });
            }
        }

        private event Init InitVars;

        private event StructureLoop FirstLoop;

        private event PostLoop PostFirstLoop;

        private event StructureLoop SecondLoop;

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        public override Error Execute()
        {
            ICity city;
            if (!locator.TryGetObjects(cityId, out city))
            {
                return Error.CityNotFound;                
            }

            var mainBuilding = city.MainBuilding;
            if (mainBuilding == null || mainBuilding.Lvl == 0)
            {
                return Error.CityNotFound;                
            }

            beginTime = SystemClock.Now;
            endTime = SystemClock.Now.AddSeconds(CalculateTime(INTERVAL_IN_SECONDS));
            return Error.Ok;
        }

        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
            ICity city;
            locker.Lock(cityId, out city).Do(() => StateChange(ActionState.Failed));
        }

        private void CreateSubscriptions()
        {
            Repair();
            Labor();
            Upkeep();
            FastIncome();
            AlignmentPoint();
            CityExpenseValue();
        }

        public override void Callback(object custom)
        {
            ICity city;
            locker.Lock(cityId, out city).Do(() =>
            {
                if (!IsValid())
                {
                    return;
                }

                if (Config.actions_skip_city_actions && !city.Owner.IsLoggedIn)
                {
                    StateChange(ActionState.Completed);
                    return;
                }

                // Prevent duplicate city actions incase of a bug
                var cityPassiveCount = city.Worker.PassiveActions.Values.Count(x => x.Type == Type);
                if (cityPassiveCount > 1)
                {
                    StateChange(ActionState.Completed);
                    return;
                }

                city.BeginUpdate();

                if (InitVars != null)
                {
                    InitVars(city);
                }

                if (FirstLoop != null)
                {
                    foreach (var structure in city)
                    {
                        FirstLoop(city, structure);
                    }
                }

                if (PostFirstLoop != null)
                {
                    PostFirstLoop(city);
                }

                if (SecondLoop != null)
                {
                    foreach (var structure in city)
                    {
                        SecondLoop(city, structure);
                    }
                }

                city.EndUpdate();

                beginTime = SystemClock.Now;
                endTime = SystemClock.Now.AddSeconds(CalculateTime(INTERVAL_IN_SECONDS));
                StateChange(ActionState.Fired);
            });
        }

        private void CityExpenseValue()
        {
            PostFirstLoop += city =>
            {
                city.ExpenseValue = formula.CalculateTotalCityExpense(city, structureFactory, technologyFactory, unitFactory);
            };
        }

        private void Labor()
        {
            PostFirstLoop += city =>
                {
                    if (city.Owner.IsIdleForAWeek)
                    {
                        return;
                    }

                    if (city.Owner.IsIdleForThreeDays) {
                        city.Resource.Gold.Rate = formula.GetGoldRate(city);
                        return;
                    }

                    laborTimeRemains += (int)CalculateTime(INTERVAL_IN_SECONDS);
                    int laborRate = formula.GetLaborRate(city.GetTotalLaborers(), city);
                    if (laborRate <= 0)
                    {
                        city.Resource.Gold.Rate = formula.GetGoldRate(city);
                        return;
                    }

                    int laborProduction = laborTimeRemains / laborRate;
                    if (laborProduction <= 0)
                    {
                        city.Resource.Gold.Rate = formula.GetGoldRate(city);
                        return;
                    }

                    laborTimeRemains -= laborProduction * laborRate;
                    city.Resource.Labor.Add(laborProduction);
                    city.Resource.Gold.Rate = formula.GetGoldRate(city);
                };
        }

        private void Repair()
        {
            ushort repairPower = 0;

            InitVars += city => repairPower = 0;

            FirstLoop += (city, structure) =>
                {
                    if (city.Owner.IsIdleForAWeek)
                    {
                        return;
                    }

                    if (objectTypeFactory.IsStructureType("RepairBuilding", structure))
                    {
                        repairPower += formula.RepairRate(structure);
                    }
                };

            SecondLoop += (city, structure) =>
                {
                    if (repairPower <= 0)
                    {
                        return;
                    }

                    if (structure.Stats.Base.Battle.MaxHp <= structure.Stats.Hp ||
                        objectTypeFactory.IsStructureType("NonRepairable", structure) ||
                        structure.State.Type == ObjectState.Battle)
                    {
                        return;
                    }

                    structure.BeginUpdate();
                    structure.Stats.Hp = (ushort)Math.Min(structure.Stats.Hp + repairPower, structure.Stats.Base.Battle.MaxHp);
                    structure.EndUpdate();
                };
        }

        private void Upkeep()
        {
            PostFirstLoop += city =>
                {                   
                    if (city.Owner.IsIdleForAWeek)
                    {
                        return;
                    }

                    city.Resource.Crop.Upkeep = procedure.UpkeepForCity(city, battleFormulas);

                    if (!Config.troop_starve)
                    {
                        return;
                    }

                    // Calculate how much crop the city is making between calls to this action
                    // Notice: if the city is starving, then GetAmountReceived returns a negative number but cropCost will be positive
                    // since we switch the signs.
                    int cropCost = -city.Resource.Crop.GetAmountReceived((int)(CalculateTime(INTERVAL_IN_SECONDS)));

                    // If cropCost is negative, the city isnt starving
                    Resource upkeepCost = new Resource(crop: Math.Max(0, cropCost));

                    if (upkeepCost.Empty)
                    {
                        return;
                    }

                    if (!city.Resource.HasEnough(upkeepCost))
                    {
                        city.Worker.DoPassive(city, actionFactory.CreateStarvePassiveAction(city.Id), false);
                    }

                    city.Resource.Subtract(upkeepCost);
                };
        }

        private void FastIncome()
        {
            PostFirstLoop += city =>
                {
                    if (!Config.resource_fast_income || Config.server_production)
                    {
                        return;
                    }

                    var resource = new Resource(15000, city.Resource.Gold.Value < 99999 ? 99999 : 0, 15000, 15000, 0);
                    city.Resource.Add(resource);
                };
        }

        private void AlignmentPoint()
        {
            PostFirstLoop += city =>
                {
                    if (city.Owner.IsIdleForAWeek)
                    {
                        return;
                    }

                    if (Math.Abs(city.AlignmentPoint - 50m) < (Config.ap_deduction_per_hour / 2))
                    {
                        city.AlignmentPoint = 50m;
                    }
                    else
                    {
                        city.AlignmentPoint += city.AlignmentPoint > 50m
                                                       ? -(Config.ap_deduction_per_hour / 2)
                                                       : (Config.ap_deduction_per_hour / 2);
                    }
                };
        }

        private delegate void Init(ICity city);

        private delegate void PostLoop(ICity city);

        private delegate void StructureLoop(ICity city, IStructure structure);
    }
}