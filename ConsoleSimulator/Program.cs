﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Data;
using Game.Setup;
using Game;
using Game.Battle;
using Game.Fighting;
using log4net.Config;
using log4net;
using System.IO;
using Game.Data.Stats;

namespace ConsoleSimulator {
    class Program {
        static void Main(string[] args) {
            Factory.CompileConfigFiles();
            // CSVToXML.Converter.Go(Config.data_folder, Config.csv_compiled_folder, Config.csv_folder);
            Factory.InitAll();
            // Load map
            using (FileStream map = new FileStream(Config.maps_folder + "map.dat", FileMode.Open)) {

                // Create region changes file or open it depending on config settings
                string regionChangesPath = Config.regions_folder + "region_changes.dat";
#if DEBUG
                bool createRegionChanges = Config.database_empty || !File.Exists(regionChangesPath);
#else
                bool createRegionChanges = !File.Exists(regionChangesPath);
#endif
                FileStream regionChanges = File.Open(regionChangesPath, createRegionChanges ? FileMode.Create : FileMode.Open, FileAccess.ReadWrite);

                // Load map
                Global.World.Load(map, regionChanges, createRegionChanges, Config.map_width, Config.map_height, Config.region_width, Config.region_height,
                                  Config.city_region_width, Config.city_region_height);
            }

            Global.DbManager.Pause();
            XmlConfigurator.Configure();
            ILog logger = LogManager.GetLogger(typeof(Program));

      //      BattleReport.WriterInit();
            File.Delete("swordsman.csv");
            Simulation sim;
            foreach( KeyValuePair<int,BaseUnitStats> kvp in UnitFactory.dict ) {
                sim = new Simulation((ushort)(kvp.Key/100), kvp.Value.Lvl, 1, Simulation.QuantityUnit.GroupSize,false);
                sim.RunDef("swordsman.csv");
                sim = new Simulation((ushort)(kvp.Key / 100), kvp.Value.Lvl, 1, Simulation.QuantityUnit.GroupSize, false);
                sim.RunAtk("swordsman.csv");
            }
        }

        static void bm_UnitRemoved(CombatObject co) {
            co.Print();            
        }

        static void bm_ExitTurn(CombatList atk, CombatList def, int turn) {
            System.Console.WriteLine("turn ends");
            System.Console.WriteLine("defenders:");
            foreach (CombatObject co in def) {
                co.Print();
            }
            System.Console.WriteLine("attackers:");
            foreach (CombatObject co in atk) {
                co.Print();
            }           
        }

        static void bm_WithdrawDefender(IEnumerable<CombatObject> list) {
            System.Console.WriteLine("defender leaving:");
            foreach (CombatObject co in list) {
                co.Print();
            }
        }

        static void bm_WithdrawAttacker(IEnumerable<CombatObject> list) {
            System.Console.WriteLine("attacker leaving:");
            foreach (CombatObject co in list) {
                co.Print();
            }
        }

        static void bm_ExitBattle(CombatList atk, CombatList def) {
            System.Console.WriteLine("battle ends");
            System.Console.WriteLine("defenders:");
            foreach (CombatObject co in def) {
                co.Print();
            }
            System.Console.WriteLine("attackers:");
            foreach (CombatObject co in atk) {
                co.Print();
            }
        }
    }
}
