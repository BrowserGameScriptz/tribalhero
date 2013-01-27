﻿#region

using System;
using System.Linq;
using Game.Battle;
using Game.Data;
using Game.Data.Troop;
using Game.Logic.Formulas;
using Game.Setup;

#endregion

namespace Game.Logic.Procedures
{
    partial class Procedure
    {
        /// <summary>
        ///     Sets the resource caps for the given city
        /// </summary>
        /// <param name="city"></param>
        public virtual void SetResourceCap(ICity city)
        {
            if (Config.resource_cap)
            {
                var bonus =
                        city.Technologies.GetEffects(EffectCode.AtticStorageMod, EffectInheritance.All)
                            .Sum(x => (int)x.Value[0]);
                var resourceBonus = formula.HiddenResource(city) * (double)bonus / 100;

                city.Resource.SetLimits(formula.ResourceCropCap(city.Lvl) + resourceBonus.Crop,
                                        0,
                                        formula.ResourceIronCap(city.Lvl) + resourceBonus.Iron,
                                        formula.ResourceWoodCap(city.Lvl) + resourceBonus.Wood,
                                        0);
            }
            else
            {
                city.Resource.SetLimits(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
            }
        }

        public int UpkeepForCity(ICity city)
        {
            var upkeep = 0;
            var effects = city.Technologies.GetEffects(EffectCode.UpkeepReduce);

            foreach (var stub in city.Troops.MyStubs())
            {
                foreach (var formation in stub)
                {
                    foreach (var kvp in formation)
                    {
                        decimal formationPenalty = formation.Type == FormationType.Garrison ? 1.25m : 1m;
                        int reduceTechSum = effects.Sum(x => BattleFormulas.Current.UnitStatModCheck(city.Template[kvp.Key].Battle,
                                                                                    TroopBattleGroup.Any,
                                                                                    (string)x.Value[1]) ? (int)x.Value[0] : 0);
                        decimal reductionPercentage = (100m - Math.Min(reduceTechSum, 30m)) / 100m;

                        upkeep += (int)Math.Ceiling(formationPenalty * kvp.Value * city.Template[kvp.Key].Upkeep * reductionPercentage);
                    }
                }
            }

            return upkeep;
        }
    }
}