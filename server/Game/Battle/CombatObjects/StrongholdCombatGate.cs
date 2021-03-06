﻿using Game.Comm;
using Game.Data;
using Game.Data.Stronghold;
using Game.Setup;
using Persistance;

namespace Game.Battle.CombatObjects
{
    public class StrongholdCombatGate : StrongholdCombatStructure
    {
        public StrongholdCombatGate(uint id,
                                    uint battleId,
                                    ushort type,
                                    byte lvl,
                                    decimal hp,
                                    IStronghold stronghold,
                                    IStructureCsvFactory structureCsvFactory,
                                    IBattleFormulas battleFormulas,
                                    IDbManager dbManager)
                : base(id, battleId, type, lvl, hp, stronghold, structureCsvFactory, battleFormulas, dbManager)
        {
        }

        public override void TakeDamage(decimal dmg, out Resource returning, out int attackPoints)
        {
            base.TakeDamage(dmg, out returning, out attackPoints);
            Stronghold.BeginUpdate();
            Stronghold.Gate = hp;
            Stronghold.EndUpdate();
        }

        public override void CalcActualDmgToBeTaken(ICombatList attackers,
                                                    ICombatList defenders,
                                                    IBattleRandom random,
                                                    decimal baseDmg,
                                                    int attackIndex,
                                                    out decimal actualDmg)
        {
            actualDmg = baseDmg / 10;
        }

        public override void AddPacketInfo(Packet packet)
        {
            base.AddPacketInfo(packet);
            packet.AddString(Stronghold.Theme);
        }
    }
}