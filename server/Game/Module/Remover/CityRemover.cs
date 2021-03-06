﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common;
using Game.Data;
using Game.Data.Tribe;
using Game.Data.Troop;
using Game.Logic;
using Game.Logic.Actions;
using Game.Map;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;
using Persistance;

namespace Game.Module.Remover
{
    public class CityRemover : ICityRemover, ISchedule
    {
        private readonly ILogger logger = LoggerFactory.Current.GetLogger<CityRemover>();

        private const double SHORT_RETRY = 5;

        private const double LONG_RETRY = 90;

        private readonly uint cityId;

        private readonly IActionFactory actionFactory;

        private readonly IWorld world;

        private readonly IScheduler scheduler;

        private readonly ILocker locker;

        private readonly ITileLocator tileLocator;

        private readonly IDbManager dbManager;

        private readonly ITroopObjectInitializerFactory troopObjectInitializerFactory;

        public CityRemover(uint cityId, 
            IActionFactory actionFactory, 
            ITileLocator tileLocator, 
            IWorld world, 
            IScheduler scheduler, 
            ILocker locker, 
            IDbManager dbManager, 
            ITroopObjectInitializerFactory troopObjectInitializerFactory)
        {
            this.cityId = cityId;
            this.actionFactory = actionFactory;

            this.world = world;
            this.scheduler = scheduler;
            this.locker = locker;
            this.tileLocator = tileLocator;
            this.dbManager = dbManager;
            this.troopObjectInitializerFactory = troopObjectInitializerFactory;
        }

        public bool Start(bool force = false)
        {
            ICity city;
            if (!world.TryGetObjects(cityId, out city))
            {
                throw new Exception("City not found");
            }

            if (!force && city.Deleted != City.DeletedState.NotDeleted)
            {
                return false;
            }

            if (city.Deleted == City.DeletedState.NotDeleted)
            {
                city.BeginUpdate();
                city.Deleted = City.DeletedState.Deleting;
                city.EndUpdate();
            }

            Time = SystemClock.Now;
            scheduler.Put(this);
            return true;
        }

        #region ISchedule Members

        public DateTime Time { get; private set; }

        public bool IsScheduled { get; set; }

        public void Callback(object custom)
        {
            ICity city;
            IStructure mainBuilding;

            if (!world.TryGetObjects(cityId, out city))
            {
                throw new Exception("City not found");
            }

            bool shouldReturn = locker.Lock(GetForeignTroopLockList, new object[] {city}, city)
                                      .Do(() =>
                                      {
                                          if (city == null)
                                          {
                                              return true;
                                          }

                                          // if local city is in battle, try again later
                                          if (city.Battle != null)
                                          {
                                              Reschedule(SHORT_RETRY);
                                              return true;
                                          }

                                          // send all stationed from other players back
                                          if (city.Troops.StationedHere().Any())
                                          {
                                              IEnumerable<ITroopStub> stationedTroops = new List<ITroopStub>(city.Troops.StationedHere());

                                              foreach (var stub in stationedTroops)
                                              {
                                                  if (RemoveForeignTroop(stub) != Error.Ok)
                                                  {
                                                      logger.Error(String.Format("removeForeignTroop failed! cityid[{0}] stubid[{1}]", city.Id, stub.StationTroopId));
                                                  }
                                              }
                                          }

                                          // If city is being targetted by an assignment, try again later
                                          var reader = dbManager.ReaderQuery(string.Format("SELECT id FROM `{0}` WHERE `location_type` = 'City' AND `location_id` = @locationId  LIMIT 1", Assignment.DB_TABLE),
                                                                             new[] {new DbColumn("locationId", city.Id, DbType.UInt32)});
                                          bool beingTargetted = reader.HasRows;
                                          reader.Close();
                                          if (beingTargetted)
                                          {
                                              Reschedule(LONG_RETRY);
                                              return true;
                                          }

                                          return false;
                                      });

            if (shouldReturn)
            {
                return;
            }

            shouldReturn = locker.Lock(GetLocalTroopLockList, new object[] {city}, city)
                                 .Do(() =>
                                 {
                                     if (city.TryGetStructure(1, out mainBuilding))
                                     {
                                         // don't continue unless all troops are either idle or stationed
                                         if (city.TroopObjects.Any() || city.Troops.Any(s => s.State != TroopState.Idle && s.State != TroopState.Stationed))
                                         {
                                             Reschedule(LONG_RETRY);
                                             return true;
                                         }

                                         // starve all troops)
                                         city.Troops.Starve(100, true);

                                         if (city.Troops.Upkeep > 0)
                                         {
                                             Reschedule(LONG_RETRY);
                                             return true;
                                         }

                                         // remove all buildings except mainbuilding
                                         if (city.Any(structure => !structure.IsMainBuilding))
                                         {
                                             city.BeginUpdate();
                                             foreach (IStructure structure in
                                                     new List<IStructure>(city).Where(structure => structure.IsBlocked == 0 && !structure.IsMainBuilding))
                                             {
                                                 structure.BeginUpdate();
                                                 world.Regions.Remove(structure);
                                                 city.ScheduleRemove(structure, false);
                                                 structure.EndUpdate();
                                             }
                                             city.EndUpdate();
                                             Reschedule(SHORT_RETRY);
                                             return true;
                                         }

                                         // remove all customized tiles
                                         foreach (var position in tileLocator.ForeachTile(city.PrimaryPosition.X, city.PrimaryPosition.Y, city.Radius))
                                         {
                                             world.Regions.RevertTileType(position.X, position.Y, true);
                                         }
                                     }

                                     return false;
                                 });

            if (shouldReturn)
            {
                return;
            }

            // remove all remaining passive action
            if (city.TryGetStructure(1, out mainBuilding))
            {
                foreach (var passiveAction in new List<PassiveAction>(city.Worker.PassiveActions.Values))
                {
                    passiveAction.WorkerRemoved(false);
                }
            }

            locker.Lock(cityId, out city).Do(() =>
            {
                if (city.Notifications.Count > 0)
                {
                    Reschedule(SHORT_RETRY);
                    return;
                }

                // remove mainbuilding
                if (city.TryGetStructure(1, out mainBuilding))
                {
                    // remove default troop
                    city.Troops.Remove(1);

                    // remove city from the region
                    world.Regions.MiniMapRegions.Remove(city);

                    mainBuilding.BeginUpdate();
                    world.Regions.Remove(mainBuilding);
                    city.ScheduleRemove(mainBuilding, false);
                    mainBuilding.EndUpdate();

                    Reschedule(SHORT_RETRY);
                    return;
                }

                // in the case of the OjectRemoveAction for mainbuilding is still there
                if (city.Worker.PassiveActions.Count > 0)
                {
                    Reschedule(SHORT_RETRY);
                    return;
                }

                logger.Info(string.Format("Player {0}:{1} City {2}:{3} Lvl {4} is deleted.",
                                          city.Owner.Name,
                                          city.Owner.PlayerId,
                                          city.Name,
                                          city.Id,
                                          city.Lvl));
                world.Cities.Remove(city);
            });
        }

        private void Reschedule(double interval)
        {
            Time = DateTime.UtcNow.AddMinutes(interval * Config.seconds_per_unit);
            scheduler.Put(this);
        }

        private static ILockable[] GetForeignTroopLockList(object[] custom)
        {
            return
                    ((ICity)custom[0]).Troops.StationedHere()
                                      .Select(stub => stub.City)
                                      .Distinct()
                                      .Cast<ILockable>()
                                      .ToArray();
        }

        private static ILockable[] GetLocalTroopLockList(object[] custom)
        {
            return
                    ((ICity)custom[0]).Troops.MyStubs()
                                      .Where(x => x.Station != null)
                                      .Select(stub => stub.Station)
                                      .Distinct()
                                      .Cast<ILockable>()
                                      .ToArray();
        }

        private Error RemoveForeignTroop(ITroopStub stub)
        {
            var troopInitializer = troopObjectInitializerFactory.CreateStationedTroopObjectInitializer(stub);            
            var ra = actionFactory.CreateRetreatChainAction(stub.City.Id, troopInitializer);
            return stub.City.Worker.DoPassive(stub.City, ra, true);
        }

        #endregion
    }
}