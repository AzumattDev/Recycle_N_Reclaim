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
        }

        public static List<InventoryGui.RecipeDataPair> get_m_recipeList(this InventoryGui instance)
        {
            return instance.m_availableRecipes;
        }

        public static List<InventoryGui.RecipeDataPair> get_m_availableRecipes(this InventoryGui instance)
        {
            return instance.m_availableRecipes;
        }

        public static InventoryGui.RecipeDataPair get_m_selectedRecipe(this InventoryGui instance)
        {
            return instance.m_selectedRecipe;
        }

        public static void set_m_selectedRecipe(this InventoryGui instance, InventoryGui.RecipeDataPair value)
        {
            instance.m_selectedRecipe = value;
        }

        public static int get_m_selectedVariant(this InventoryGui instance)
        {
            return instance.m_selectedVariant;
        }

        public static void set_m_selectedVariant(this InventoryGui instance, int value)
        {
            instance.m_selectedVariant = value;
        }

        public static float get_m_craftTimer(this InventoryGui instance)
        {
            return instance.m_craftTimer;
        }

        public static void set_m_craftTimer(this InventoryGui instance, float value)
        {
            instance.m_craftTimer = value;
        }

        public static Color get_m_minStationLevelBasecolor(this InventoryGui instance)
        {
            return instance.m_minStationLevelBasecolor;
        }
    }
}