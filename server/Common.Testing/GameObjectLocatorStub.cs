﻿using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Data;
using Game.Data.BarbarianTribe;
using Game.Data.Stronghold;
using Game.Data.Tribe;
using Game.Data.Troop;
using Game.Map;

namespace Common.Testing
{
    public class GameObjectLocatorStub : IGameObjectLocator
    {
        private readonly Dictionary<uint, IBattleManager> battles = new Dictionary<uint, IBattleManager>();

        private readonly Dictionary<uint, ICity> cities = new Dictionary<uint, ICity>();

        private readonly Dictionary<uint, IPlayer> players = new Dictionary<uint, IPlayer>();

        private readonly Dictionary<uint, IStronghold> strongholds = new Dictionary<uint, IStronghold>();

        private readonly Dictionary<uint, ITribe> tribes = new Dictionary<uint, ITribe>();

        private readonly Dictionary<uint, IBarbarianTribe> barbarianTribes = new Dictionary<uint, IBarbarianTribe>();
        
        private readonly Dictionary<Tuple<uint, uint>, IGameObject> gameObjects = new Dictionary<Tuple<uint, uint>, IGameObject>();

        public GameObjectLocatorStub(params object[] objects)
        {
            Add(objects);
        }

        public void Add(params object[] objects)
        {
            foreach (var o in objects)
            {
                ICity city = o as ICity;
                if (city != null)
                {
                    cities.Add(city.Id, city);
                }

                IPlayer player = o as IPlayer;
                if (player != null)
                {
                    players.Add(player.PlayerId, player);
                }

                ITribe tribe = o as ITribe;
                if (tribe != null)
                {
                    tribes.Add(tribe.Id, tribe);
                }

                IBattleManager battle = o as IBattleManager;
                if (battle != null)
                {
                    battles.Add(battle.BattleId, battle);
                }

                IStronghold stronghold = o as IStronghold;
                if (stronghold != null)
                {
                    strongholds.Add(stronghold.ObjectId, stronghold);
                }

                IBarbarianTribe barbarianTribe = o as IBarbarianTribe;
                if (barbarianTribe != null)
                {
                    barbarianTribes.Add(barbarianTribe.ObjectId, barbarianTribe);
                }

                IGameObject gameObject = o as IGameObject;
                if (gameObject != null)
                {

                    gameObjects.Add(new Tuple<uint, uint>(gameObject.GroupId, gameObject.ObjectId), gameObject);
                }
            }
        }

        public IRegionManager Regions { get; set; }

        public bool TryGetObjects(uint cityId, out ICity city)
        {
            return cities.TryGetValue(cityId, out city);
        }

        public bool TryGetObjects(uint playerId, out IPlayer player)
        {
            return players.TryGetValue(playerId, out player);
        }

        public bool TryGetObjects(uint tribeId, out ITribe tribe)
        {
            return tribes.TryGetValue(tribeId, out tribe);
        }

        public bool TryGetObjects(uint battleId, out IBattleManager battleManager)
        {
            return battles.TryGetValue(battleId, out battleManager);
        }

        public bool TryGetObjects(uint cityId, uint structureId, out ICity city, out IStructure structure)
        {
            structure = null;
            return cities.TryGetValue(cityId, out city) && (city.TryGetStructure(structureId, out structure) || GetGameObject(cityId, structureId, out structure));
        }

        public bool TryGetObjects(uint cityId, uint troopObjectId, out ICity city, out ITroopObject troopObject)
        {
            troopObject = null;
            return cities.TryGetValue(cityId, out city) && (city.TryGetTroop(troopObjectId, out troopObject)|| GetGameObject(cityId, troopObjectId, out troopObject));
        }

        public bool TryGetObjects(uint strongholdId, out IStronghold stronghold)
        {
            return strongholds.TryGetValue(strongholdId, out stronghold);
        }

        public bool TryGetObjects(uint barbarianTribeId, out IBarbarianTribe barbarianTribe)
        {
            return barbarianTribes.TryGetValue(barbarianTribeId, out barbarianTribe);
        }

        private bool GetGameObject<T>(uint cityId, uint objectId, out T obj) where T : IGameObject
        {
            obj = default(T);

            IGameObject gameObject;
            if (!gameObjects.TryGetValue(new Tuple<uint, uint>(cityId, objectId), out gameObject))
            {
                return false;
            }

            obj = (T)gameObject;
            return true;
        }
    }
}