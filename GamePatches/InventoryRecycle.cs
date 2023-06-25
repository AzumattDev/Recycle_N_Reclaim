using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Jewelcrafting;
using Recycle_N_Reclaim.YAMLStuff;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Recycle_N_Reclaim.GamePatches;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateItemDrag))]
public static class UpdateItemDragPatch
{
    // Caching Reflection Calls
    internal static Type epicLootType = Recycle_N_ReclaimPlugin.epicLootAssembly?.GetType("EpicLoot.ItemDataExtensions");
    internal static MethodInfo isMagicMethod = epicLootType?.GetMethod("IsMagic", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ItemDrop.ItemData) }, null);
    internal static MethodInfo getRarityMethod = epicLootType?.GetMethod("GetRarity", BindingFlags.Public | BindingFlags.Static);
    internal static Type enchantTabControllerType = Recycle_N_ReclaimPlugin.epicLootAssembly?.GetType("EpicLoot.Crafting.EnchantTabController");
    internal static MethodInfo getEnchantCostsMethod = enchantTabControllerType?.GetMethod("GetEnchantCosts", BindingFlags.Public | BindingFlags.Static);

    private static void Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount, ref GameObject ___m_dragGo)
    {
        if (Recycle_N_ReclaimPlugin.lockToAdmin.Value == Recycle_N_ReclaimPlugin.Toggle.On && !Recycle_N_ReclaimPlugin.ConfigSyncVar.IsAdmin)
        {
            return;
        }

        if (Recycle_N_ReclaimPlugin.discardInvEnabled.Value == Recycle_N_ReclaimPlugin.Toggle.Off || !Recycle_N_ReclaimPlugin.hotKey.Value.IsDown() || ___m_dragItem == null || !___m_dragInventory.ContainsItem(___m_dragItem))
            return;

        Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_dropPrefab.name}");

        if (Recycle_N_ReclaimPlugin.returnResources.Value > 0)
        {
            Recipe recipe = ObjectDB.instance.GetRecipe(___m_dragItem);

            if (recipe != null && (Recycle_N_ReclaimPlugin.returnUnknownResources.Value == Recycle_N_ReclaimPlugin.Toggle.On || Player.m_localPlayer.IsRecipeKnown(___m_dragItem.m_shared.m_name)))
            {
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Recipe stack: {recipe.m_amount} num of stacks: {___m_dragAmount / recipe.m_amount}");

                List<Piece.Requirement>? reqs = recipe.m_resources.ToList();

                bool isMagic = false;
                bool cancel = false;
                if (Recycle_N_ReclaimPlugin.epicLootAssembly != null && Recycle_N_ReclaimPlugin.returnEnchantedResources.Value == Recycle_N_ReclaimPlugin.Toggle.On)
                    isMagic = (bool)isMagicMethod?.Invoke(null, new[] { ___m_dragItem });

                if (isMagic)
                {
                    int rarity = (int)getRarityMethod?.Invoke(null, new[] { ___m_dragItem });
                    List<KeyValuePair<ItemDrop, int>> magicReqs =
                        (List<KeyValuePair<ItemDrop, int>>)getEnchantCostsMethod?.Invoke(null, new object[] { ___m_dragItem, rarity });

                    foreach (KeyValuePair<ItemDrop, int> kvp in magicReqs)
                    {
                        var recipe2 = ObjectDB.instance.GetRecipe(kvp.Key.m_itemData);
                        bool isRecipeKnown = recipe2 != null && Player.m_localPlayer.IsRecipeKnown(kvp.Key.m_itemData.m_shared.m_name);
                        bool isKnownMaterial = Player.m_localPlayer.m_knownMaterial.Contains(kvp.Key.m_itemData.m_shared.m_name);

                        if (Recycle_N_ReclaimPlugin.returnUnknownResources.Value == Recycle_N_ReclaimPlugin.Toggle.Off &&
                            (!isRecipeKnown || !isKnownMaterial))
                        {
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You don't know all the recipes for this item's materials.");
                            return;
                        }

                        reqs.Add(new Piece.Requirement
                        {
                            m_amount = recipe2 != null ? recipe2.m_amount : kvp.Value,
                            m_resItem = kvp.Key
                        });
                    }
                }


                if (Jewelcrafting.API.IsLoaded() && Recycle_N_ReclaimPlugin.returnEnchantedResources.Value == Recycle_N_ReclaimPlugin.Toggle.On)
                {
                    if (Jewelcrafting.API.GetGems(___m_dragItem).Any())
                    {
                        var gemsOnItem = Jewelcrafting.API.GetGems(___m_dragItem);

                        Dictionary<ItemDrop, ItemDrop.ItemData> gemItemData = gemsOnItem
                            .Where(gem => gem != null)
                            .Select(gem => ObjectDB.instance.GetItemPrefab(gem.gemPrefab).GetComponent<ItemDrop>())
                            .Where(itemDrop => itemDrop != null)
                            .ToDictionary(itemDrop => itemDrop, itemDrop => itemDrop.m_itemData);

                        foreach (var gemItem in gemItemData)
                        {
                            var recipe3 = ObjectDB.instance.GetRecipe(gemItem.Value);
                            bool isRecipeKnown = recipe3 != null && Player.m_localPlayer.IsRecipeKnown(gemItem.Value.m_shared.m_name);
                            bool isKnownMaterial = Player.m_localPlayer.m_knownMaterial.Contains(gemItem.Value.m_shared.m_name);

                            if (Recycle_N_ReclaimPlugin.returnUnknownResources.Value == Recycle_N_ReclaimPlugin.Toggle.Off && (!isRecipeKnown || !isKnownMaterial))
                            {
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You don't know all the recipes for this item's materials.");
                                return;
                            }

                            reqs.Add(new Piece.Requirement
                            {
                                m_amount = recipe3 != null ? recipe3.m_amount : gemItem.Value.m_stack,
                                m_resItem = gemItem.Key
                            });
                        }
                    }
                }


                if (!cancel && ___m_dragAmount / recipe.m_amount > 0)
                    for (int i = 0; i < ___m_dragAmount / recipe.m_amount; i++)
                        foreach (Piece.Requirement req in reqs)
                        {
                            int quality = ___m_dragItem.m_quality;
                            for (int j = quality; j > 0; j--)
                            {
                                GameObject prefab = ObjectDB.instance.m_items.FirstOrDefault(item => item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == req.m_resItem.m_itemData.m_shared.m_name)!;
                                ItemDrop.ItemData newItem = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                                int numToAdd = Mathf.RoundToInt(req.GetAmount(j) * Recycle_N_ReclaimPlugin.returnResources.Value);
                                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug(($"Returning {numToAdd}/{req.GetAmount(j)} {prefab.name}"));

                                while (numToAdd > 0)
                                {
                                    int stack = Mathf.Min(req.m_resItem.m_itemData.m_shared.m_maxStackSize, numToAdd);
                                    numToAdd -= stack;

                                    if (!GroupUtils.IsPrefabExcludedInInventory(prefab.name))
                                    {
                                        if (Player.m_localPlayer.GetInventory().AddItem(prefab.name, stack, req.m_resItem.m_itemData.m_quality, req.m_resItem.m_itemData.m_variant, 0, "") == null)
                                        {
                                            Transform transform1;
                                            ItemDrop component = GameObject.Instantiate(prefab, (transform1 = Player.m_localPlayer.transform).position + transform1.forward + transform1.up, transform1.rotation).GetComponent<ItemDrop>();
                                            component.m_itemData = newItem;
                                            component.m_itemData.m_dropPrefab = prefab;
                                            component.m_itemData.m_stack = stack;
                                            component.Save();
                                        }
                                    }
                                }
                            }
                        }
            }
        }

        if (___m_dragAmount == ___m_dragItem.m_stack)
        {
            Player.m_localPlayer.RemoveEquipAction(___m_dragItem);
            Player.m_localPlayer.UnequipItem(___m_dragItem, false);
            ___m_dragInventory.RemoveItem(___m_dragItem);
        }
        else
        {
            ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
        }

        Object.Destroy(___m_dragGo);
        ___m_dragGo = null;
        __instance.UpdateCraftingPanel();
    }
}