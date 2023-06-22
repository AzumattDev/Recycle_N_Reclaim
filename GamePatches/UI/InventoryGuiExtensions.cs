using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Recycle_N_Reclaim.GamePatches.UI
{
    public static class InventoryGuiExtensions
    {
        public static float get_m_recipeListBaseSize(this InventoryGui instance)
        {
            return instance.m_recipeListBaseSize;
            return (float)AccessTools.Field(typeof(InventoryGui), "m_recipeListBaseSize").GetValue(instance);
        }

        public static List<GameObject> get_m_recipeList(this InventoryGui instance)
        {
            return instance.m_recipeList;
            return (List<GameObject>)AccessTools.Field(typeof(InventoryGui), "m_recipeList").GetValue(instance);
        }

        public static List<KeyValuePair<Recipe, ItemDrop.ItemData>> get_m_availableRecipes(this InventoryGui instance)
        {
            return instance.m_availableRecipes;
            return (List<KeyValuePair<Recipe, ItemDrop.ItemData>>)AccessTools.Field(typeof(InventoryGui),
                "m_availableRecipes").GetValue(instance);
        }

        public static KeyValuePair<Recipe, ItemDrop.ItemData> get_m_selectedRecipe(this InventoryGui instance)
        {
            return instance.m_selectedRecipe;
            return (KeyValuePair<Recipe, ItemDrop.ItemData>)AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe")
                .GetValue(instance);
        }

        public static void set_m_selectedRecipe(this InventoryGui instance, KeyValuePair<Recipe, ItemDrop.ItemData> value)
        {
            instance.m_selectedRecipe = value;
            //AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe").SetValue(instance, value);
        }

        public static int get_m_selectedVariant(this InventoryGui instance)
        {
            return instance.m_selectedVariant;
            return (int)AccessTools.Field(typeof(InventoryGui), "m_selectedVariant").GetValue(instance);
        }

        public static void set_m_selectedVariant(this InventoryGui instance, int value)
        {
            instance.m_selectedVariant = value;
            //AccessTools.Field(typeof(InventoryGui), "m_selectedVariant").SetValue(instance, value);
        }

        public static float get_m_craftTimer(this InventoryGui instance)
        {
            return instance.m_craftTimer;
            return (float)AccessTools.Field(typeof(InventoryGui), "m_craftTimer").GetValue(instance);
        }

        public static void set_m_craftTimer(this InventoryGui instance, float value)
        {
            instance.m_craftTimer = value;
            //AccessTools.Field(typeof(InventoryGui), "m_craftTimer").SetValue(instance, value);
        }

        public static Color get_m_minStationLevelBasecolor(this InventoryGui instance)
        {
            return instance.m_minStationLevelBasecolor;
            return (Color)AccessTools.Field(typeof(InventoryGui), "m_minStationLevelBasecolor").GetValue(instance);
        }
    }
}