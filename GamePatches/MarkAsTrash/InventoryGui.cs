using HarmonyLib;
using UnityEngine;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
static class InventoryGuiShowPatch
{
    // slightly lower priority so we get rendered on top of equipment slot mods
    [HarmonyPriority(Priority.LowerThanNormal)]
    static void Postfix(InventoryGui __instance)
    {
        ButtonRenderer.hasOpenedInventoryOnce = true;

        ButtonRenderer.MainButtonUpdate.UpdateInventoryGuiButtons(__instance);
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
static class InventoryGuiHidePatch
{
    [HarmonyPriority(Priority.LowerThanNormal)]
    static void Postfix(InventoryGui __instance)
    {
        if (!__instance.m_animator.GetBool("visible"))
            return;
        // reset in case player forgot to turn it off
        TrashingMode.HasCurrentlyToggledTrashing = false;
        UserConfig.ResetAllTrashing();
    }
}

// Isn't working at the moment like it does in my other mod
/*[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.GetHoveredElement))]
static class InventoryGridGetHoveredElementPatch
{
    [HarmonyPriority(Priority.VeryHigh)]
    static void Postfix(InventoryGrid __instance, ref InventoryGrid.Element __result)
    {
        if (TrashingMode.IsInTrashingMode() && __result != null)
        {
            // If my left click is down
            if (Recycle_N_ReclaimPlugin.TrashingKeybind.Value.IsKeyHeld())
            {
#if DEBUG
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Hovered element: {__result.m_pos}");
#endif
                // Only add if there is an item in the slot
                if (__result.m_used)
                {
                    Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Hovered element: {__result.m_pos}");
                    UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID()).AddSlotTrashing(__result.m_pos);
                }
            }
        }
    }
}*/