using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EntityShelteredBuff
{

    [HarmonyPatch(typeof(EntityStats))]
    [HarmonyPatch("UpdateWeatherStats")]
    public class UpdateWeatherStats
    {
        static void Postfix(
            EntityStats __instance,
            float ___m_amountEnclosed)
        {
            EntityAlive entity = __instance.Entity;
            EntityBuffs buffs = entity.Buffs;
            if (___m_amountEnclosed > 0.9f)
            {
                if (buffs.HasBuff("buffElementSheltered")) return;
                Log.Out("+ buffElementSheltered");
                buffs.AddBuff("buffElementSheltered");
            }
            else
            {
                if (!buffs.HasBuff("buffElementSheltered")) return;
                Log.Out("- buffElementSheltered");
                buffs.RemoveBuff("buffElementSheltered");
            }
        }
    }

}
