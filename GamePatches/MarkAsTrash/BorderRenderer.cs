using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
static class InventoryGridUpdateGuiPatch
{
    public static Sprite border = null!;

    [HarmonyAfter("Azumatt.AzuAutoStore", "goldenrevolver.quick_stack_store")]
    internal static void Postfix(Player player, Inventory ___m_inventory, List<InventoryGrid.Element> ___m_elements)
    {
        if (player == null || player.m_inventory != ___m_inventory)
        {
            return;
        }

        int width = ___m_inventory.GetWidth();
        UserConfig playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());

        for (int y = 0; y < ___m_inventory.GetHeight(); ++y)
        {
            for (int x = 0; x < ___m_inventory.GetWidth(); ++x)
            {
                int index = y * width + x;

                Image img;
                if (___m_elements[index].m_queued.transform.childCount > 0 && global::Utils.FindChild(___m_elements[index].m_queued.transform, "RecycleNReclaimBorderImage")?.GetComponent<Image>() is { } borderImage)
                {
                    img = borderImage;
                }
                else
                {
                    img = CreateBorderImage(___m_elements[index].m_queued);
                }

                if (img != null)
                {
                    img.color = Recycle_N_ReclaimPlugin.BorderColorTrashedSlot.Value;
                    img.enabled = playerConfig.IsSlotTrashed(new Vector2i(x, y));
                }
            }
        }

        if (Recycle_N_ReclaimPlugin.TrashingModifierKeybind1.Value.IsKeyHeld() && Recycle_N_ReclaimPlugin.TrashingKeybind.Value.IsKeyDown())
        {
            List<ItemDrop.ItemData> itemsToRecycle = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData>? list = ___m_inventory.GetAllItems();
            for (int i = 0; i < list.Count; ++i)
            {
                ItemDrop.ItemData? item = list[i];
                if (UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID()).IsSlotTrashed(item.m_gridPos))
                {
                    itemsToRecycle.Add(item);
                }
            }

            for (int index = 0; index < itemsToRecycle.Count; ++index)
            {
                ItemDrop.ItemData? itemData = itemsToRecycle[index];
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Recycling item at grid position {itemData.m_gridPos}");
                Utils.InventoryRecycleItem(itemData, itemData.m_stack, ___m_inventory, null, null);
            }

            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Resetting all MarkAsTrash data!");
            UserConfig.ResetAllTrashing();
        }


        /*if (Recycle_N_ReclaimPlugin.TrashingModifierKeybind1.Value.IsKeyHeld() && Recycle_N_ReclaimPlugin.TrashingKeybind.Value.IsKeyDown())
        {
            List<ItemDrop.ItemData>? list = ___m_inventory.GetAllItems();
            for (int i = 0; i < list.Count; ++i)
            {
                ItemDrop.ItemData itemData = list[i];
                if (UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID()).IsSlotTrashed(itemData.m_gridPos))
                {
                    Utils.InventoryRecycleItem(itemData, itemData.m_stack, ___m_inventory, null, null);
                }
            }

            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Resetting all MarkAsTrash data!");
            UserConfig.ResetAllTrashing();
        }*/

        if (!Recycle_N_ReclaimPlugin.TrashingModifierKeybind1.Value.IsKeyHeld())
        {
            // reset in case player forgot to turn it off
            TrashingMode.HasCurrentlyToggledTrashing = false;
            UserConfig.ResetAllTrashing();
        }
    }

    private static Image CreateBorderImage(Image baseImg)
    {
        // set m_queued parent as parent first, so the position is correct
        Image? obj = Object.Instantiate(baseImg, baseImg.transform.parent);

        // destroy all children of the newly created clone of m_queued image (as other mods add children to it too)
        foreach (Transform child in obj.transform)
        {
            Object.Destroy(child.gameObject);
        }

        // Set the name to something unique so we can find it later, and be compatible with other mods
        obj.name = "RecycleNReclaimBorderImage";
        // change the parent to the m_queued image so we can access the new image without a loop
        obj.transform.SetParent(baseImg.transform);
        // set the new border image
        obj.sprite = border;

        return obj;
    }
}

public static class ItemDataExtension
{
    public static int GridVectorToGridIndex(this ItemDrop.ItemData item, int width)
    {
        return item.m_gridPos.y * width + item.m_gridPos.x;
    }
}