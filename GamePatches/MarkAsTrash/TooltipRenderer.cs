using System;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash
{
    [HarmonyPatch(typeof(ItemDrop.ItemData))]
    internal static class TooltipRenderer
    {
        [HarmonyPatch(nameof(ItemDrop.ItemData.GetTooltip), new Type[]
        {
            typeof(ItemDrop.ItemData),
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(int)
        })]
        [HarmonyPostfix]
        public static void GetTooltip(ItemDrop.ItemData item, bool crafting, ref string __result)
        {
            if (crafting || !Recycle_N_ReclaimPlugin.DisplayTooltipHint.Value)
            {
                return;
            }

            StringBuilder stringBuilder = new StringBuilder(256);
            stringBuilder.Append(__result);

            UserConfig conf = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());

            if (conf.IsSlotTrashed(item.m_gridPos))
            {
                string? color = ColorUtility.ToHtmlStringRGB(Recycle_N_ReclaimPlugin.BorderColorTrashedSlot.Value);

                stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{Recycle_N_ReclaimPlugin.TrashedSlotTooltip.Value}</color>");
            }

            __result = stringBuilder.ToString();
        }
    }
}