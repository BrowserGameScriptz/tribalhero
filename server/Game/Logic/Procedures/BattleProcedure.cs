#region

using System;
using System.Linq;
using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Data.Troop;
using Game.Setup;
using Game.Util;

#endregion

namespace Game.Logic.Procedures
{
    public class BattleProcedure
    {
        private readonly ICombatGroupFactory combatGroupFactory;

        private readonly ICombatUnitFactory combatUnitFactory;

        [Obsolete("For testing only", true)]
        protected BattleProcedure()
        {
        }

        public BattleProcedure(ICombatUnitFactory combatUnitFactory,
                               ICombatGroupFactory combatGroupFactory)
        {
            this.combatUnitFactory = combatUnitFactory;
            this.combatGroupFactory = combatGroupFactory;            
        }
        
        public virtual bool IsNewbieProtected(IPlayer player)
        {
            return SystemClock.Now.Subtract(player.Created).TotalSeconds < Config.newbie_protection;
        }

        public virtual ICombatGroup AddAttackerToBattle(IBattleManager battleManager, ITroopObject troopObject)
        {
            var offensiveGroup = combatGroupFactory.CreateCityOffensiveCombatGroup(battleManager.BattleId,
                                                                                   battleManager.GetNextGroupId(),
                                                                                   troopObject);
            foreach (var attackCombatUnits in troopObject.Stub
                                                         .SelectMany(formation => formation)
                                                         .Select(kvp => combatUnitFactory.CreateAttackCombatUnit(battleManager,
                                                                                                                 troopObject,
                                                                                                                 FormationType.Attack,
                                                                                                                 kvp.Key,
                                                                                                                 kvp.Value)))
            {
                attackCombatUnits.ToList().ForEach(offensiveGroup.Add);
            }

            battleManager.Add(offensiveGroup, BattleManager.BattleSide.Attack, true);

            return offensiveGroup;
        }

        public virtual uint AddReinforcementToBattle(IBattleManager battleManager, ITroopStub stub, FormationType formationToAddToBattle)
        {
            stub.BeginUpdate();
            stub.Template.LoadStats(TroopBattleGroup.Defense);
            stub.EndUpdate();

            var defensiveGroup = combatGroupFactory.CreateCityDefensiveCombatGroup(battleManager.BattleId,
                                                                                   battleManager.GetNextGroupId(),
                                                                                   stub);
            foreach (var kvp in stub[formationToAddToBattle])
            {
                combatUnitFactory.CreateDefenseCombatUnit(battleManager, stub, formationToAddToBattle, kvp.Key, kvp.Value)
                                 .ToList()
                                 .ForEach(defensiveGroup.Add);
            }
            battleManager.Add(defensiveGroup, BattleManager.BattleSide.Defense, true);

            return defensiveGroup.Id;
        }

        /// <summary>
        ///     Repairs all structures up to max HP but depends on percentage from sense of urgency effect
        /// </summary>
        /// <param name="city"></param>
        /// <param name="maxHp"></param>
        public virtual void SenseOfUrgency(ICity city, uint maxHp)
        {
            int healPercent = Math.Min(100, city.Technologies.GetEffects(EffectCode.SenseOfUrgency).Sum(x => (int)x.Value[0]));

            if (healPercent == 0)
            {
                return;
            }

            var restore = Math.Floor(maxHp * (healPercent / 100m));

            foreach (var structure in city.Where(structure =>
                                                 structure.State.Type == ObjectState.Normal &&
                                                 structure.Stats.Hp != structure.Stats.Base.Battle.MaxHp))
            {
                structure.BeginUpdate();
                structure.Stats.Hp += restore;
                structure.EndUpdate();
            }
        }

        public virtual bool HasTooManyAttacks(ICity city)
        {
            return city.Worker.PassiveActions.Values.Count(action => action.Category == ActionCategory.Attack) > 40;
        }

        public virtual bool HasTooManyDefenses(ICity city)
        {
            return city.Worker.PassiveActions.Values.Count(action => action.Category == ActionCategory.Defense) > 40;
        }
    }
}