using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Recycle_N_Reclaim.YAMLStuff;

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
static class PredefinedGroupGrab
{
    static void Postfix(ObjectDB __instance)
    {
        if (!ZNetScene.instance)
            return;
        GroupUtils.CreatePredefinedGroups(__instance);
    }
}

public class GroupUtils
{
    // Get a list of all excluded groups for a container
    public static List<string> GetExcludedGroups(string container)
    {
        if (Recycle_N_ReclaimPlugin.yamlData.Containers.TryGetValue(container, out var containerObj))
        {
            return containerObj.Exclude.Where(excludeItem =>
                    Recycle_N_ReclaimPlugin.yamlData.Groups.ContainsKey(excludeItem))
                .ToList();
        }

        return new List<string>();
    }

    public static bool IsGroupDefined(string groupName)
    {
        return Recycle_N_ReclaimPlugin.yamlData.Groups.ContainsKey(groupName);
    }


// Check if a group exists in the container data
    public static bool GroupExists(string groupName)
    {
        return Recycle_N_ReclaimPlugin.yamlData.Groups.ContainsKey(groupName);
    }

// Get a list of all groups in the container data
    public static List<string> GetAllGroups()
    {
        return Recycle_N_ReclaimPlugin.yamlData.Groups.Keys.ToList();
    }

// Get a list of all items in a group
    public static List<string> GetItemsInGroup(string groupName)
    {
        if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(groupName, out var groupObj))
        {
            return groupObj.ToList();
        }

        return new List<string>();
    }

    public static string GetPrefabName(string name)
    {
        char[] anyOf = new char[2] { '(', ' ' };
        int length = name.IndexOfAny(anyOf);
        return length < 0 ? name : name.Substring(0, length);
    }

    internal static GameObject? GetItemPrefabFromGameObject(ItemDrop itemDropComponent, GameObject inputGameObject)
    {
        GameObject? itemPrefab = ObjectDB.instance.GetItemPrefab(GetPrefabName(inputGameObject.name));
        itemDropComponent.m_itemData.m_dropPrefab = itemPrefab;
        return itemPrefab != null ? itemPrefab : null;
    }

    internal static bool CheckItemDropIntegrity(ItemDrop itemDropComp)
    {
        if (itemDropComp.m_itemData == null) return false;
        return itemDropComp.m_itemData.m_shared != null;
    }

    internal static void CreatePredefinedGroups(ObjectDB __instance)
    {
        foreach (GameObject gameObject in __instance.m_items.Where(x => x.GetComponentInChildren<ItemDrop>() != null))
        {
            var itemDrop = gameObject.GetComponentInChildren<ItemDrop>();
            if (!CheckItemDropIntegrity(itemDrop)) continue;
            var drop = GetItemPrefabFromGameObject(itemDrop, gameObject);
            itemDrop.m_itemData.m_dropPrefab = itemDrop.gameObject; // Fix all drop prefabs to be the actual item
            if (drop != null)
            {
                ItemDrop.ItemData.SharedData sharedData = itemDrop.m_itemData.m_shared;
                string groupName = "";

                if (sharedData.m_food > 0.0 && sharedData.m_foodStamina > 0.0)
                {
                    groupName = "Food";
                }

                if (sharedData.m_food > 0.0 && sharedData.m_foodStamina == 0.0)
                {
                    groupName = "Potion";
                }
                else if (sharedData.m_itemType == ItemDrop.ItemData.ItemType.Fish)
                {
                    groupName = "Fish";
                }

                switch (sharedData.m_itemType)
                {
                    case ItemDrop.ItemData.ItemType.OneHandedWeapon or ItemDrop.ItemData.ItemType.TwoHandedWeapon
                        or ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft or ItemDrop.ItemData.ItemType.Bow:
                        switch (sharedData.m_skillType)
                        {
                            case Skills.SkillType.Swords:
                                groupName = "Swords";
                                break;
                            case Skills.SkillType.Bows:
                                groupName = "Bows";
                                break;
                            case Skills.SkillType.Crossbows:
                                groupName = "Crossbows";
                                break;
                            case Skills.SkillType.Axes:
                                groupName = "Axes";
                                break;
                            case Skills.SkillType.Clubs:
                                groupName = "Clubs";
                                break;
                            case Skills.SkillType.Knives:
                                groupName = "Knives";
                                break;
                            case Skills.SkillType.Pickaxes:
                                groupName = "Pickaxes";
                                break;
                            case Skills.SkillType.Polearms:
                                groupName = "Polearms";
                                break;
                            case Skills.SkillType.Spears:
                                groupName = "Spears";
                                break;
                        }

                        break;
                    case ItemDrop.ItemData.ItemType.Torch:
                        groupName = "Equipment";
                        break;
                    case ItemDrop.ItemData.ItemType.Trophy:
                        string[] bossTrophies =
                            { "eikthyr", "elder", "bonemass", "dragonqueen", "goblinking", "SeekerQueen" };
                        groupName = bossTrophies.Any(sharedData.m_name.EndsWith) ? "Boss Trophy" : "Trophy";
                        break;
                    case ItemDrop.ItemData.ItemType.Material:
                        if (ObjectDB.instance.GetItemPrefab("Cultivator").GetComponent<ItemDrop>().m_itemData.m_shared
                                .m_buildPieces.m_pieces.FirstOrDefault(p =>
                                {
                                    Piece.Requirement[] requirements = p.GetComponent<Piece>().m_resources;
                                    return requirements.Length == 1 &&
                                           requirements[0].m_resItem.m_itemData.m_shared.m_name == sharedData.m_name;
                                }) is { } piece)
                        {
                            groupName = piece.GetComponent<Plant>()?.m_grownPrefabs[0].GetComponent<Pickable>()
                                ?.m_amount > 1
                                ? "Crops"
                                : "Seeds";
                        }

                        if (ZNetScene.instance.GetPrefab("smelter").GetComponent<Smelter>().m_conversion
                            .Any(c => c.m_from.m_itemData.m_shared.m_name == sharedData.m_name))
                        {
                            groupName = "Ores";
                        }

                        if (ZNetScene.instance.GetPrefab("smelter").GetComponent<Smelter>().m_conversion
                            .Any(c => c.m_to.m_itemData.m_shared.m_name == sharedData.m_name))
                        {
                            groupName = "Metals";
                        }

                        if (ZNetScene.instance.GetPrefab("blastfurnace").GetComponent<Smelter>().m_conversion
                            .Any(c => c.m_from.m_itemData.m_shared.m_name == sharedData.m_name))
                        {
                            groupName = "Ores";
                        }

                        if (ZNetScene.instance.GetPrefab("blastfurnace").GetComponent<Smelter>().m_conversion
                            .Any(c => c.m_to.m_itemData.m_shared.m_name == sharedData.m_name))
                        {
                            groupName = "Metals";
                        }

                        if (ZNetScene.instance.GetPrefab("charcoal_kiln").GetComponent<Smelter>().m_conversion
                            .Any(c => c.m_from.m_itemData.m_shared.m_name == sharedData.m_name))
                        {
                            groupName = "Woods";
                        }

                        if (sharedData.m_name == "$item_elderbark")
                        {
                            groupName = "Woods";
                        }

                        break;
                }

                if (!string.IsNullOrEmpty(groupName))
                {
                    AddItemToGroup(groupName, itemDrop);
                }

                if (sharedData != null)
                {
                    groupName = "All";
                    AddItemToGroup(groupName, itemDrop);
                }
            }
        }
    }

    private static void AddItemToGroup(string groupName, ItemDrop itemDrop)
    {
        // Check if the group exists, and if not, create it
        if (!GroupExists(groupName))
        {
            Recycle_N_ReclaimPlugin.yamlData.Groups[groupName] = new List<string>();
        }

        // Add the item to the group
        string prefabName = Utils.GetPrefabName(itemDrop.m_itemData.m_dropPrefab);
        if (Recycle_N_ReclaimPlugin.yamlData.Groups[groupName].Contains(prefabName)) return;
        Recycle_N_ReclaimPlugin.yamlData.Groups[groupName].Add(prefabName);
        Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"(CreatePredefinedGroups) Added {prefabName} to {groupName}");
    }

    public static bool IsPrefabExcludedInReclaiming(string prefabName)
    {
        return IsPrefabExcludedInEntity(Recycle_N_ReclaimPlugin.yamlData.Reclaiming, prefabName);
    }

    public static bool IsPrefabExcludedInInventory(string prefabName)
    {
        return IsPrefabExcludedInEntity(Recycle_N_ReclaimPlugin.yamlData.Inventory, prefabName);
    }

    public static bool IsPrefabExcludedInContainer(string containerName, string prefabName)
    {
        if (Recycle_N_ReclaimPlugin.yamlData.Containers.TryGetValue(containerName, out var container))
        {
            return IsPrefabExcludedInEntity(container, prefabName);
        }

        // If container not found, it's not excluded
        return false;
    }

    private static bool IsPrefabExcludedInEntity(object entity, string prefabName)
    {
        if (entity is tainer container)
        {
            if (container.IncludeOverride != null)
            {
                // Check if the prefab is in the included items or groups, if includeOverride is true
                foreach (var item in container.IncludeOverride)
                {
                    // Direct match
                    if (item == prefabName)
                    {
                        return false; // It's included, so it can't be excluded
                    }

                    // Check if it's a group
                    if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(item, out var group))
                    {
                        if (group.Contains(prefabName))
                        {
                            return false; // It's included in a group, so it can't be excluded
                        }
                    }
                }
            }

            if (container.Exclude != null)
            {
                // Check if the prefab is in the excluded items list of the container
                foreach (var item in container.Exclude)
                {
                    // Direct match
                    if (item == prefabName)
                    {
                        return true;
                    }

                    // Check if it's a group
                    if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(item, out var group))
                    {
                        if (group.Contains(prefabName))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException("The exclude field is required for a container that has been defined.");
            }
        }
        else if (entity is Reclaiming reclaiming)
        {
            if (reclaiming.IncludeOverride != null)
            {
                // Check if the prefab is in the included items or groups, if includeOverride is true
                foreach (var item in reclaiming.IncludeOverride)
                {
                    // Direct match
                    if (item == prefabName)
                    {
                        return false; // It's included, so it can't be excluded
                    }

                    // Check if it's a group
                    if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(item, out var group))
                    {
                        if (group.Contains(prefabName))
                        {
                            return false; // It's included in a group, so it can't be excluded
                        }
                    }
                }
            }

            if (reclaiming.Exclude != null)
            {
                // Check if the prefab is in the excluded items list of reclaiming
                foreach (var item in reclaiming.Exclude)
                {
                    // Direct match
                    if (item == prefabName)
                    {
                        return true;
                    }

                    // Check if it's a group
                    if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(item, out var group))
                    {
                        if (group.Contains(prefabName))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException("The exclude field is required for reclaiming.");
            }
        }
        else if (entity is Inventory inventory)
        {
            if (inventory.Exclude != null)
            {
                // Check if the prefab is in the excluded items list of the inventory
                foreach (var item in inventory.Exclude)
                {
                    // Direct match
                    if (item == prefabName)
                    {
                        return true;
                    }

                    // Check if it's a group
                    if (Recycle_N_ReclaimPlugin.yamlData.Groups.TryGetValue(item, out var group))
                    {
                        if (group.Contains(prefabName))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException("The exclude field is required for inventory.");
            }
        }

        // If not found in any of the excluded items or groups, it's not excluded
        return false;
    }
}