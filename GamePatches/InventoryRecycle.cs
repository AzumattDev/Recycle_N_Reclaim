using Object = UnityEngine.Object;

namespace Recycle_N_Reclaim.GamePatches;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateItemDrag))]
public static class UpdateItemDragPatch
{
    // Caching Reflection Calls
    internal static readonly Type? epicLootType = epicLootAssembly?.GetType("EpicLoot.ItemDataExtensions");
    internal static readonly MethodInfo? isMagicMethod = epicLootType?.GetMethod("IsMagic", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ItemDrop.ItemData) }, null);
    internal static readonly MethodInfo? getRarityMethod = epicLootType?.GetMethod("GetRarity", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ItemDrop.ItemData) }, null);
    internal static readonly Type? enchantTabControllerType = epicLootAssembly?.GetType("EpicLoot.Crafting.EnchantHelper");
    internal static readonly MethodInfo? getEnchantCostsMethod = enchantTabControllerType?.GetMethod("GetEnchantCosts", BindingFlags.Public | BindingFlags.Static);

    private static void Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount, ref GameObject ___m_dragGo)
    {
        if (lockToAdmin.Value == Recycle_N_ReclaimPlugin.Toggle.On && !ConfigSyncVar.IsAdmin)
        {
            return;
        }

        if (discardInvEnabled.Value == Recycle_N_ReclaimPlugin.Toggle.Off || !hotKey.Value.IsDown() || ___m_dragItem == null || !___m_dragInventory.ContainsItem(___m_dragItem))
            return;

        Recycle_N_ReclaimLogger.LogDebug($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_dropPrefab.name}");

        Utils.InventoryRecycleItem(___m_dragItem, ___m_dragAmount, ___m_dragInventory, __instance, ___m_dragGo);

        Object.Destroy(___m_dragGo);
        ___m_dragGo = null;
        __instance.UpdateCraftingPanel();
    }
}