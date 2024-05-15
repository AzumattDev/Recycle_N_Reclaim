using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jewelcrafting;
using Recycle_N_Reclaim.YAMLStuff;
using UnityEngine;

namespace Recycle_N_Reclaim.GamePatches;

public static class Utils
{
    public static Texture2D LoadTextureFromResources(string pathName, string folderName = "Assets")
    {
        var myAssembly = typeof(Recycle_N_ReclaimPlugin).Assembly;
        var myStream = myAssembly.GetManifestResourceStream($"{typeof(Recycle_N_ReclaimPlugin).Namespace}.{folderName}.{pathName}");
        byte[] bytes;
        var tex2D = new Texture2D(2, 2);
        var emptyTex2D = new Texture2D(2, 2);

        if (myStream == null)
        {
            return emptyTex2D;
        }

        using (var binaryReader = new BinaryReader(myStream))
        {
            bytes = binaryReader.ReadBytes((int)myStream.Length);
        }

        return tex2D.LoadImage(bytes) ? tex2D : emptyTex2D;
    }

    public static void InventoryRecycleItem(ItemDrop.ItemData ___m_dragItem, int ___m_dragAmount, Inventory ___m_dragInventory, InventoryGui __instance, GameObject ___m_dragGo)
    {
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
                    isMagic = (bool)UpdateItemDragPatch.isMagicMethod?.Invoke(null, new[] { ___m_dragItem });

                if (isMagic)
                {
                    int rarity = (int)UpdateItemDragPatch.getRarityMethod?.Invoke(null, new[] { ___m_dragItem });
                    List<KeyValuePair<ItemDrop, int>> magicReqs = (List<KeyValuePair<ItemDrop, int>>)UpdateItemDragPatch.getEnchantCostsMethod?.Invoke(null, new object[] { ___m_dragItem, rarity });

                    foreach (KeyValuePair<ItemDrop, int> kvp in magicReqs)
                    {
                        var recipe2 = ObjectDB.instance.GetRecipe(kvp.Key.m_itemData);
                        bool isRecipeKnown = recipe2 != null && Player.m_localPlayer.IsRecipeKnown(kvp.Key.m_itemData.m_shared.m_name);
                        bool isKnownMaterial = Player.m_localPlayer.m_knownMaterial.Contains(kvp.Key.m_itemData.m_shared.m_name);

                        if (Recycle_N_ReclaimPlugin.returnUnknownResources.Value == Recycle_N_ReclaimPlugin.Toggle.Off &&
                            (!isRecipeKnown || !isKnownMaterial))
                        {
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Recycle_N_ReclaimPlugin.Localize("$azumatt_recycle_n_reclaim_no_material_recipes"));
                            return;
                        }

                        reqs.Add(new Piece.Requirement
                        {
                            m_amount = recipe2 != null ? recipe2.m_amount : kvp.Value,
                            m_resItem = kvp.Key
                        });
                    }
                }


                if (API.IsLoaded() && Recycle_N_ReclaimPlugin.returnEnchantedResources.Value == Recycle_N_ReclaimPlugin.Toggle.On)
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
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Recycle_N_ReclaimPlugin.Localize("$azumatt_recycle_n_reclaim_no_material_recipes"));
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

                                    if (!GroupUtils.IsPrefabExcludedInInventory(global::Utils.GetPrefabName(prefab)))
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
    }
}