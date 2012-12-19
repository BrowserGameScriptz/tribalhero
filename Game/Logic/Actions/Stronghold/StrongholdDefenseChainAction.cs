#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Data.Stronghold;
using Game.Data.Troop;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;

#endregion

namespace Game.Logic.Actions
{
    public class StrongholdDefenseChainAction : ChainAction
    {
        private readonly IActionFactory actionFactory;

        private readonly BattleProcedure battleProcedure;

        private readonly uint cityId;

        private readonly Formula formula;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly ILocker locker;

        private readonly AttackMode mode;

        private readonly Procedure procedure;

        private readonly uint targetStrongholdId;

        private readonly uint troopObjectId;

        public StrongholdDefenseChainAction(uint cityId,
                                            uint troopObjectId,
                                            uint targetStrongholdId,
                                            AttackMode mode,
                                            IActionFactory actionFactory,
                                            Procedure procedure,
                                            ILocker locker,
                                            IGameObjectLocator gameObjectLocator,
                                            BattleProcedure battleProcedure,
                                            Formula formula)
        {
            this.cityId = cityId;
            this.targetStrongholdId = targetStrongholdId;
            this.troopObjectId = troopObjectId;
            this.mode = mode;
            this.actionFactory = actionFactory;
            this.procedure = procedure;
            this.locker = locker;
            this.gameObjectLocator = gameObjectLocator;
            this.battleProcedure = battleProcedure;
            this.formula = formula;
        }

        public StrongholdDefenseChainAction(uint id,
                                            string chainCallback,
                                            PassiveAction current,
                                            ActionState chainState,
                                            bool isVisible,
                                            IDictionary<string, string> properties,
                                            IActionFactory actionFactory,
                                            Procedure procedure,
                                            ILocker locker,
                                            IGameObjectLocator gameObjectLocator,
                                            BattleProcedure battleProcedure,
                                            Formula formula)
                : base(id, chainCallback, current, chainState, isVisible)
        {
            this.actionFactory = actionFactory;
            this.procedure = procedure;
            this.locker = locker;
            this.gameObjectLocator = gameObjectLocator;
            this.battleProcedure = battleProcedure;
            this.formula = formula;
            cityId = uint.Parse(properties["city_id"]);
            troopObjectId = uint.Parse(properties["troop_object_id"]);
            mode = (AttackMode)uint.Parse(properties["mode"]);
            targetStrongholdId = uint.Parse(properties["target_stronghold_id"]);
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.StrongholdDefenseChain;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                        {
                                new XmlKvPair("city_id", cityId), new XmlKvPair("troop_object_id", troopObjectId),
                                new XmlKvPair("target_stronghold_id", targetStrongholdId),
                                new XmlKvPair("mode", (byte)mode)
                        });
            }
        }

        public override ActionCategory Category
        {
            get
            {
                return ActionCategory.Defense;
            }
        }

        public override Error Execute()
        {
            ICity city;
            ITroopObject troopObject;
            IStronghold targetStronghold;

            if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject) ||
                !gameObjectLocator.TryGetObjects(targetStrongholdId, out targetStronghold))
            {
                return Error.ObjectNotFound;
            }

            if (!troopObject.Stub.HasFormation(FormationType.Defense))
            {
                return Error.Unexpected;                
            }

            if (battleProcedure.HasTooManyDefenses(city))
            {
                return Error.TooManyTroops;
            }

            var canStrongholdBeDefended = battleProcedure.CanStrongholdBeDefended(city, targetStronghold);
            if (canStrongholdBeDefended != Error.Ok)
            {
                return canStrongholdBeDefended;
            }

            //Load the units stats into the stub
            troopObject.Stub.BeginUpdate();
            troopObject.Stub.Template.LoadStats(TroopBattleGroup.Defense);
            troopObject.Stub.RetreatCount = (ushort)formula.GetAttackModeTolerance(troopObject.Stub.TotalCount, mode);
            troopObject.Stub.EndUpdate();

            city.References.Add(troopObject, this);

            city.Notifications.Add(troopObject, this);

            var tma = actionFactory.CreateTroopMovePassiveAction(cityId,
                                                                 troopObject.ObjectId,
                                                                 targetStronghold.X,
                                                                 targetStronghold.Y,
                                                                 false,
                                                                 true);

            ExecuteChainAndWait(tma, AfterTroopMoved);

            if (targetStronghold.Tribe != null)
            {
                targetStronghold.Tribe.SendUpdate();
            }

            return Error.Ok;
        }

        private void AfterTroopMoved(ActionState state)
        {
            if (state == ActionState.Fired)
            {
                // Verify the target is still good, otherwise we walk back immediately

                ICity city;
                IStronghold targetStronghold;
                ITroopObject troopObject;

                if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
                {
                    throw new Exception("City or troop object is missing");
                }

                if (!gameObjectLocator.TryGetObjects(targetStrongholdId, out targetStronghold))
                {
                    CancelCurrentChain();
                    return;
                }

                bool invalidTarget;
                using (locker.Lock(city, targetStronghold))
                {
                    invalidTarget = battleProcedure.CanStrongholdBeDefended(city, targetStronghold) != Error.Ok;
                }

                // If the stronghold is not there or we are unable to attack/defense it, then cancel the current TroopMoveAction
                if (invalidTarget)
                {
                    CancelCurrentChain();
                }
            }
            else if (state == ActionState.Failed)
            {
                // If TroopMove failed it's because we cancelled it and the target is invalid. Walk back home
                ICity city;
                ITroopObject troopObject;

                using (locker.Lock(cityId, troopObjectId, out city, out troopObject))
                {
                    TroopMovePassiveAction tma = actionFactory.CreateTroopMovePassiveAction(city.Id,
                                                                                            troopObject.ObjectId,
                                                                                            city.X,
                                                                                            city.Y,
                                                                                            true,
                                                                                            true);
                    ExecuteChainAndWait(tma, AfterTroopMovedHome);
                }
            }
            else if (state == ActionState.Completed)
            {
                ICity city;
                IStronghold targetStronghold;
                ITroopObject troopObject;

                if (!gameObjectLocator.TryGetObjects(cityId, troopObjectId, out city, out troopObject))
                {
                    throw new Exception("City or troop object is missing");
                }

                if (!gameObjectLocator.TryGetObjects(targetStrongholdId, out targetStronghold))
                {
                    throw new Exception("Stronghold is missing");
                }

                CallbackLock.CallbackLockHandler lockAll = delegate { return targetStronghold.LockList.ToArray(); };

                using (locker.Lock(lockAll, null, city, targetStronghold))
                {
                    if (targetStronghold.Tribe == city.Owner.Tribesman.Tribe)
                    {
                        if (targetStronghold.MainBattle != null)
                        {
                            battleProcedure.AddReinforcementToBattle(targetStronghold.MainBattle, troopObject.Stub, FormationType.Defense);
                        }

                        StationTroopInStronghold(troopObject, targetStronghold);
                    }
                    else
                    {
                        // Walk back to city if none of the above conditions apply
                        TroopMovePassiveAction tma = actionFactory.CreateTroopMovePassiveAction(city.Id,
                                                                                                troopObject.ObjectId,
                                                                                                city.X,
                                                                                                city.Y,
                                                                                                true,
                                                                                                true);
                        ExecuteChainAndWait(tma, AfterTroopMovedHome);
                    }
                }
            }
        }

        private void StationTroopInStronghold(ITroopObject troopObject, IStronghold stronghold)
        {
            procedure.TroopObjectStation(troopObject, stronghold);
            StateChange(ActionState.Completed);
        }

        private void AfterTroopMovedHome(ActionState state)
        {
            if (state == ActionState.Completed)
            {
                ICity city;
                ITroopObject troopObject;
                using (locker.Lock(cityId, troopObjectId, out city, out troopObject))
                {
                    // If city is not in battle then add back to city otherwise join local battle
                    if (city.Battle == null)
                    {
                        procedure.TroopObjectDelete(troopObject, true);
                        StateChange(ActionState.Completed);
                    }
                    else
                    {
                        var eda = actionFactory.CreateCityEngageDefensePassiveAction(cityId, troopObject.ObjectId, FormationType.Defense);
                        ExecuteChainAndWait(eda, AfterCityEngageDefense);
                    }
                }
            }
        }

        private void AfterCityEngageDefense(ActionState state)
        {
            if (state == ActionState.Completed)
            {
                ICity city;
                ITroopObject troopObject;
                using (locker.Lock(cityId, troopObjectId, out city, out troopObject))
                {
                    procedure.TroopObjectDelete(troopObject, troopObject.Stub.TotalCount != 0);
                    StateChange(ActionState.Completed);
                }
            }
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }
    }
}