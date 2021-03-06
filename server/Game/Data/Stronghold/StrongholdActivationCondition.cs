﻿using Game.Setup;

namespace Game.Data.Stronghold
{
    class StrongholdActivationCondition : IStrongholdActivationCondition
    {
        private readonly int cityPerLevel = Config.stronghold_cities_per_level;

        public bool ShouldActivate(IStronghold stronghold)
        {
            return stronghold.NearbyCitiesCount >= cityPerLevel * stronghold.Lvl;
        }

        public int Score(IStronghold stronghold)
        {
            return stronghold.NearbyCitiesCount - stronghold.Lvl;
        }
    }
}