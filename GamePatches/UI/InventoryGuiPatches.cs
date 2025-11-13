namespace Recycle_N_Reclaim.GamePatches.UI;

[HarmonyPatch]
public static class InventoryGuiPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
    [HarmonyPriority(600)]
    static void OnTabCraftPressedAlsoEnableRecycling1(InventoryGui __instance)
    {
        RecyclingTabButtonHolder.SetInteractable(true);
        // temporary fix for compatibility with EpicLoot
        __instance.UpdateCraftingPanel();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
    static bool UpdateCraftingPanelDetourOnRecyclingTab(InventoryGui __instance)
    {
        if (RecyclingTabButtonHolder.InRecycleTab()) return false;
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    static void OnHideSetToCraftingTab(InventoryGui __instance)
    {
        if (RecyclingTabButtonHolder == null || !RecyclingTabButtonHolder.InRecycleTab()) return;
        InventoryGui.instance.OnTabCraftPressed();
        RecyclingTabButtonHolder.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
    static void UpdateCraftingPanelDetourOnOtherTabsEnableRecyclingButton(InventoryGui __instance)
    {
        bool inRecycleTab = RecyclingTabButtonHolder.InRecycleTab();

        if (inRecycleTab) return;
        var player = Player.m_localPlayer;
        RecyclingTabButtonHolder.SetInteractable(true);
        if (!player.GetCurrentCraftingStation() && !player.NoCostCheat())
        {
            RecyclingTabButtonHolder.SetActive(false);
            return;
        }

        RecyclingTabButtonHolder.SetActive(true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe), typeof(Player), typeof(float))]
    static bool UpdateRecipeOnRecyclingTab(InventoryGui __instance, Player player, float dt)
    {
        if (!RecyclingTabButtonHolder.InRecycleTab()) return true;
        RecyclingTabButtonHolder.UpdateRecipe(player, dt);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
    static void InventorySave(Inventory __instance)
    {
        if (RecyclingTabButtonHolder == null || !RecyclingTabButtonHolder.InRecycleTab()) return;
        if (__instance == Player.m_localPlayer.GetInventory())
        {
            RecyclingTabButtonHolder.UpdateRecyclingList();
            InventoryGui.instance.SetRecipe(-1, false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem), typeof(ItemDrop.ItemData), typeof(bool))]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem), typeof(ItemDrop.ItemData), typeof(bool))]
    static void HumanoidEquip(Humanoid __instance)
    {
        if (RecyclingTabButtonHolder == null || __instance != Player.m_localPlayer || !RecyclingTabButtonHolder.InRecycleTab()) return;
        if (__instance.GetInventory() == Player.m_localPlayer.GetInventory())
        {
            RecyclingTabButtonHolder.UpdateRecyclingList();
            InventoryGui.instance.SetRecipe(-1, false);
        }
    }
}