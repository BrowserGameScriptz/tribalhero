#region

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Battle;
using Game.Battle.CombatGroups;
using Game.Battle.CombatObjects;
using Game.Data;
using Game.Data.Stronghold;
using Game.Logic.Formulas;
using Game.Logic.Procedures;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;
using JsonFx.Json;
using Persistance;

#endregion

namespace Game.Logic.Actions
{
    public class StrongholdGateBattlePassiveAction : ScheduledPassiveAction
    {
        private readonly StrongholdBattleProcedure strongholdBattleProcedure;

        private readonly IDbManager dbManager;

        private readonly Formula formula;

        private readonly IGameObjectLocator gameObjectLocator;

        private readonly ILocker locker;

        private uint strongholdId;

        private Dictionary<uint, decimal> tribeDamageDealt = new Dictionary<uint, decimal>();

        private readonly IWorld world;

        private uint localGroupId;

        public StrongholdGateBattlePassiveAction(StrongholdBattleProcedure strongholdBattleProcedure,
                                                 ILocker locker,
                                                 IGameObjectLocator gameObjectLocator,
                                                 IDbManager dbManager,
                                                 Formula formula,
                                                 IWorld world)
        {
            this.strongholdBattleProcedure = strongholdBattleProcedure;
            this.locker = locker;
            this.gameObjectLocator = gameObjectLocator;
            this.dbManager = dbManager;
            this.formula = formula;
            this.world = world;
        }

        public StrongholdGateBattlePassiveAction(uint strongholdId,
                                                 StrongholdBattleProcedure strongholdBattleProcedure,
                                                 ILocker locker,
                                                 IGameObjectLocator gameObjectLocator,
                                                 IDbManager dbManager,
                                                 Formula formula,
                                                 IWorld world)
            : this(strongholdBattleProcedure, locker, gameObjectLocator, dbManager, formula, world)
        {
            this.strongholdId = strongholdId;

            IStronghold stronghold;
            if (!gameObjectLocator.TryGetObjects(strongholdId, out stronghold))
            {
                throw new Exception("Did not find stronghold that was supposed to be having a battle");
            }

            stronghold.GateBattle.GroupKilled += BattleOnGroupKilled;
            stronghold.GateBattle.ActionAttacked += BattleOnActionAttacked;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
            localGroupId = uint.Parse(properties["local_group_id"]);

            tribeDamageDealt = new JsonReader().Read<Dictionary<string, decimal>>(properties["tribe_damage_dealt"])
                                               .ToDictionary(k => uint.Parse(k.Key), v => v.Value);

            strongholdId = uint.Parse(properties["stronghold_id"]);

            IStronghold stronghold;
            if (!gameObjectLocator.TryGetObjects(strongholdId, out stronghold))
            {
                throw new Exception("Did not find stronghold that was supposed to be having a battle");
            }

            stronghold.GateBattle.GroupKilled += BattleOnGroupKilled;
            stronghold.GateBattle.ActionAttacked += BattleOnActionAttacked;
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.StrongholdGateBattlePassive;
            }
        }

        public override string Properties
        {
            get
            {
                return
                        XmlSerializer.Serialize(new[]
                        {
                                new XmlKvPair("stronghold_id", strongholdId), new XmlKvPair("local_group_id", localGroupId),
                                new XmlKvPair("tribe_damage_dealt", new JsonWriter().Write(tribeDamageDealt))
                        });
            }
        }

        private void BattleOnActionAttacked(IBattleManager battle,
                                            BattleManager.BattleSide attackingSide,
                                            ICombatGroup attackerGroup,
                                            ICombatObject attacker,
                                            ICombatGroup targetGroup,
                                            ICombatObject target,
                                            decimal damage,
                                            int attackerCount,
                                            int targetCount)
        {
            if (attackingSide != BattleManager.BattleSide.Attack || attackerGroup.Owner.Type != BattleOwnerType.City)
            {
                return;
            }

            ICity attackingCity;
            if (!gameObjectLocator.TryGetObjects(attackerGroup.Owner.Id, out attackingCity))
            {
                throw new Exception("Attacker city not found");
            }

            if (!attackingCity.Owner.IsInTribe)
            {
                return;
            }

            var tribeId = attackingCity.Owner.Tribesman.Tribe.Id;

            if (tribeDamageDealt.ContainsKey(tribeId))
            {
                tribeDamageDealt[tribeId] += damage;
            }
            else
            {
                tribeDamageDealt[tribeId] = damage;
            }
        }

        private void BattleOnGroupKilled(IBattleManager battle, ICombatGroup group)
        {
            IStronghold stronghold;
            if (!gameObjectLocator.TryGetObjects(strongholdId, out stronghold))
            {
                throw new Exception();
            }

            if (group.Id == localGroupId)
            {
                ICity loopCity = null;
                var attackerTribes = (from combatGroup in battle.Attackers
                                      where
                                              combatGroup.Owner.Type == BattleOwnerType.City &&
                                              gameObjectLocator.TryGetObjects(combatGroup.Owner.Id, out loopCity) &&
                                              loopCity.Owner.IsInTribe
                                      select loopCity.Owner.Tribesman.Tribe).Distinct().ToDictionary(k => k.Id, v => v);

                var tribesByDamage =
                        (from kv in tribeDamageDealt
                         orderby kv.Value descending
                         select new {TribeId = kv.Key, Damage = kv.Value});

                var winningTribe = tribesByDamage.FirstOrDefault(x => attackerTribes.ContainsKey(x.TribeId));

                // Open the gate to the tribe that won unless the tribe that won is
                // the tribe that owns the stronghold. This can happen if members switch tribes during an attack to the tribe
                // that owns the SH.
                if (winningTribe != null && attackerTribes[winningTribe.TribeId] != stronghold.Tribe)
                {
                    stronghold.BeginUpdate();
                    stronghold.GateOpenTo = attackerTribes[winningTribe.TribeId];
                    stronghold.EndUpdate();
                }
            }
        }

        public override void Callback(object custom)
        {
            IStronghold stronghold;
            if (!gameObjectLocator.TryGetObjects(strongholdId, out stronghold))
            {
                throw new Exception("Stronghold is missing");
            }

            CallbackLock.CallbackLockHandler lockHandler = delegate { return stronghold.LockList().ToArray(); };

            locker.Lock(lockHandler, null, stronghold).Do(() =>
            {
                if (stronghold.GateBattle.ExecuteTurn())
                {
                    // Battle continues, just save it and reschedule
                    dbManager.Save(stronghold.GateBattle);
                    endTime = SystemClock.Now.AddSeconds(formula.GetGateBattleInterval(stronghold));
                    StateChange(ActionState.Fired);
                    return;
                }

                // Battle has ended
                // Delete the battle
                stronghold.GateBattle.GroupKilled -= BattleOnGroupKilled;
                stronghold.GateBattle.ActionAttacked -= BattleOnActionAttacked;

                world.Remove(stronghold.GateBattle);
                dbManager.Delete(stronghold.GateBattle);
                stronghold.BeginUpdate();
                stronghold.GateBattle = null;
                stronghold.State = GameObjectStateFactory.NormalState();
                // Heal the gate if no one made through otherwise we let it be healed after the main battle
                if (stronghold.GateOpenTo == null)
                {
                    stronghold.GateMax = (int)formula.StrongholdGateLimit(stronghold.Lvl);
                    stronghold.Gate = Math.Max(Math.Min(stronghold.GateMax, stronghold.Gate), formula.StrongholdGateHealHp(stronghold.StrongholdState, stronghold.Lvl));
                }
                stronghold.EndUpdate();

                StateChange(ActionState.Completed);
            });
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        public override Error Execute()
        {
            IStronghold stronghold;
            if (!gameObjectLocator.TryGetObjects(strongholdId, out stronghold))
            {
                return Error.ObjectNotFound;
            }

            world.Add(stronghold.GateBattle);
            dbManager.Save(stronghold.GateBattle);

            //Add gate to battle
            var combatGroup = strongholdBattleProcedure.AddStrongholdGateToBattle(stronghold.GateBattle, stronghold);
            localGroupId = combatGroup.Id;

            stronghold.BeginUpdate();
            stronghold.State = GameObjectStateFactory.BattleState(stronghold.GateBattle.BattleId);
            stronghold.EndUpdate();

            beginTime = SystemClock.Now;
            endTime = SystemClock.Now.Add(formula.GetBattleDelayStartInterval());

            return Error.Ok;
        }

        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
            throw new Exception("City removed during battle?");
        }
    }
}