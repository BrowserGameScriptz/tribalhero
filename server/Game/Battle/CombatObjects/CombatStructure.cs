#region

using System;
using System.Collections.Generic;
using System.Data;
using Game.Comm;
using Game.Data;
using Game.Data.Stats;
using Game.Data.Troop;
using Game.Logic.Actions;
using Game.Logic.Formulas;
using Game.Map;
using Persistance;

#endregion

namespace Game.Battle.CombatObjects
{
    public class CombatStructure : CityCombatObject, ICombatStructure
    {
        public const string DB_TABLE = "combat_structures";

        private readonly IActionFactory actionFactory;

        private readonly ITileLocator tileLocator;

        private readonly Formula formula;

        private readonly byte lvl;

        private readonly BattleStats stats;

        private readonly ushort type;
        
        public string Theme { get; set; }

        /// <summary>
        ///     Since the structure HP can change from the outside.
        ///     We Need to keep a copy track of the hp that will be used for the battle. This creates some discrepancy
        ///     between the structure's HP on the outside world and in the battle but that's okay.
        /// </summary>
        private decimal hp;

        private readonly IRegionManager regionManager;

        public CombatStructure(uint id,
                               uint battleId,
                               IStructure structure,
                               BattleStats stats,
                               Formula formula,
                               IActionFactory actionFactory,
                               IBattleFormulas battleFormulas,
                               ITileLocator tileLocator,
                               IRegionManager regionManager,
                               IDbManager dbManager)
                : base(id, battleId, battleFormulas, dbManager)
        {
            this.stats = stats;
            this.formula = formula;
            this.actionFactory = actionFactory;
            this.tileLocator = tileLocator;
            this.regionManager = regionManager;
            Structure = structure;
            type = structure.Type;
            lvl = structure.Lvl;
            hp = structure.Stats.Hp;
            Theme = structure.Theme;
        }

        public CombatStructure(uint id,
                               uint battleId,
                               IStructure structure,
                               BattleStats stats,
                               decimal hp,
                               ushort type,
                               byte lvl,
                               string theme,
                               Formula formula,
                               IActionFactory actionFactory,
                               IBattleFormulas battleFormulas,
                               ITileLocator tileLocator,
                               IRegionManager regionManager, 
                               IDbManager dbManager)
                : base(id, battleId, battleFormulas, dbManager)
        {
            Structure = structure;
            this.formula = formula;
            this.actionFactory = actionFactory;
            this.tileLocator = tileLocator;
            this.regionManager = regionManager;
            this.stats = stats;
            this.hp = hp;
            this.type = type;
            this.lvl = lvl;
            this.Theme = theme;
        }

        public override ICity City
        {
            get
            {
                return Structure.City;
            }
        }

        public override ITroopStub TroopStub
        {
            get
            {
                return City.DefaultTroop;
            }
        }

        public virtual short Stamina
        {
            get
            {
                return -1;
            }
        }

        public override byte Size
        {
            get
            {
                return Structure.Stats.Base.Size;
            }
        }

        public IStructure Structure { get; private set; }

        public override BattleStats Stats
        {
            get
            {
                return stats;
            }
        }

        public override uint Visibility
        {
            get
            {
                return Stats.Rng;
            }
        }

        public override BattleClass ClassType
        {
            get
            {
                return BattleClass.Structure;
            }
        }

        public override bool IsDead
        {
            get
            {
                return hp == 0;
            }
        }

        public override ushort Count
        {
            get
            {
                return (ushort)(hp > 0 ? 1 : 0);
            }
        }

        public override byte Lvl
        {
            get
            {
                return lvl;
            }
        }

        public override ushort Type
        {
            get
            {
                return type;
            }
        }

        public override Resource Loot
        {
            get
            {
                return new Resource();
            }
        }

        public override decimal Hp
        {
            get
            {
                return hp;
            }
        }

        public override int Upkeep
        {
            get
            {
                return BattleFormulas.GetUnitsPerStructure(Structure.Lvl) / 5;
            }
        }

        public override string DbTable
        {
            get
            {
                return DB_TABLE;
            }
        }

        public override DbColumn[] DbPrimaryKey
        {
            get
            {
                return new[] {new DbColumn("battle_id", BattleId, DbType.UInt32), new DbColumn("id", Id, DbType.UInt32)};
            }
        }

        public override IEnumerable<DbDependency> DbDependencies
        {
            get
            {
                return new DbDependency[] {};
            }
        }

        public override DbColumn[] DbColumns
        {
            get
            {
                return new[]
                       {
                               new DbColumn("last_round", LastRound, DbType.UInt32),
                               new DbColumn("rounds_participated", RoundsParticipated, DbType.UInt32),
                               new DbColumn("damage_dealt", DmgDealt, DbType.Decimal),
                               new DbColumn("damage_received", DmgRecv, DbType.Decimal),
                               new DbColumn("group_id", GroupId, DbType.UInt32),
                               new DbColumn("structure_city_id", Structure.City.Id, DbType.UInt32),
                               new DbColumn("structure_id", Structure.ObjectId, DbType.UInt32),
                               new DbColumn("hp", hp, DbType.Decimal),
                               new DbColumn("type", type, DbType.UInt16),
                               new DbColumn("level", lvl, DbType.Byte),
                               new DbColumn("max_hp", stats.MaxHp, DbType.Decimal),
                               new DbColumn("attack", stats.Atk, DbType.Decimal),
                               new DbColumn("splash", stats.Splash, DbType.Byte),
                               new DbColumn("range", stats.Rng, DbType.Byte),
                               new DbColumn("stealth", stats.Stl, DbType.Byte),
                               new DbColumn("speed", stats.Spd, DbType.Byte),
                               new DbColumn("hits_dealt", HitDealt, DbType.UInt16),
                               new DbColumn("hits_dealt_by_unit", HitDealtByUnit, DbType.UInt32),
                               new DbColumn("hits_received", HitRecv, DbType.UInt16),
                               new DbColumn("is_waiting_to_join_battle", IsWaitingToJoinBattle, DbType.Boolean),
                               new DbColumn("theme_id", Theme, DbType.String)
                       };
            }
        }

        public override bool InRange(ICombatObject obj)
        {
            if (obj.ClassType == BattleClass.Unit)
            {
                return tileLocator.IsOverlapping(obj.Location(), obj.Size, obj.AttackRadius(),
                                                 Structure.PrimaryPosition, Structure.Size, Structure.Stats.Base.Radius);
            }

            throw new Exception(string.Format("Why is a structure trying to kill a unit of type {0}?",
                                              obj.GetType().FullName));
        }

        public override Position Location()
        {
            return Structure.PrimaryPosition;
        }

        public override byte AttackRadius()
        {
            return Structure.Stats.Base.Radius;
        }

        public override void CalcActualDmgToBeTaken(ICombatList attackers,
                                                    ICombatList defenders,
                                                    IBattleRandom random,
                                                    decimal baseDmg,
                                                    int attackIndex,
                                                    out decimal actualDmg)
        {
            // Miss chance
            actualDmg = BattleFormulas.GetDmgWithMissChance(attackers.UpkeepExcludingWaitingToJoinBattle, defenders.UpkeepExcludingWaitingToJoinBattle, baseDmg, random);

            // Splash dmg reduction
            actualDmg = BattleFormulas.SplashReduction(this, actualDmg, attackIndex);

            // AP Bonuses
            if (City.AlignmentPoint >= 90m)
            {
                actualDmg *= .1m;
            }
        }

        public override void TakeDamage(decimal dmg, out Resource returning, out int attackPoints)
        {
            attackPoints = 0;

            hp = Math.Max(0, hp - dmg);

            Structure.BeginUpdate();
            Structure.Stats.Hp = hp;
            Structure.EndUpdate();

            if (hp == 0)
            {
                attackPoints = formula.GetStructureKilledAttackPoint(type, lvl);
            }

            returning = null;
        }

        public override int LootPerRound()
        {
            return 0;
        }

        public override void ExitBattle()
        {
            base.ExitBattle();

            Structure.BeginUpdate();
            Structure.State = GameObjectStateFactory.NormalState();
            Structure.EndUpdate();

            // Remove structure from the world if our combat object died
            if (hp > 0)
            {
                return;
            }

            ICity city = Structure.City;

            var lockedRegions = regionManager.LockMultitileRegions(Structure.PrimaryPosition.X, Structure.PrimaryPosition.Y, Structure.Size);
            if (Structure.Lvl > 1)
            {
                Structure.City.Worker.DoPassive(Structure.City,
                                                actionFactory.CreateStructureDowngradePassiveAction(Structure.City.Id,
                                                                                                    Structure.ObjectId),
                                                false);
            }
            else
            {
                Structure.BeginUpdate();
                regionManager.Remove(Structure);
                city.ScheduleRemove(Structure, true);
                Structure.EndUpdate();
            }
            regionManager.UnlockRegions(lockedRegions);
        }

        public override void ReceiveReward(int attackPoints, Resource resource)
        {
            City.BeginUpdate();
            City.DefensePoint += attackPoints;
            City.EndUpdate();
        }

        public override void AddPacketInfo(Packet packet)
        {
            base.AddPacketInfo(packet);
            packet.AddString(Theme);
        }
    }
}