using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recycle_N_Reclaim.GamePatches.Recycling
{
    public static class Reclaimer
    {
        public static void RecycleInventoryForAllRecipes(Inventory inventory, Player player)
        {
            var itemListSnapshot = new List<ItemDrop.ItemData>();
            // copy the inventory, otherwise collection will constantly change causing issues
            itemListSnapshot.AddRange(inventory.GetAllItems());
            var analysisList = new List<RecyclingAnalysisContext>();
            for (var index = 0; index < itemListSnapshot.Count; ++index)
            {
                var item = itemListSnapshot[index];
                var analysisContext = new RecyclingAnalysisContext(item);
                analysisList.Add(analysisContext);
                RecycleOneItemInInventory(analysisContext, inventory, player);
                if (analysisContext.ShouldErrorDumpAnalysis || Recycle_N_ReclaimPlugin.DebugAlwaysDumpAnalysisContext.Value == Recycle_N_ReclaimPlugin.Toggle.On)
                {
                    analysisContext.Dump();
                }
            }

            var stringBuilder = new StringBuilder();
            foreach (var analysisContext in analysisList.Where(analysis => analysis.RecyclingImpediments.Count > 0))
            {
                stringBuilder.AppendLine($"Could not recycle {Recycle_N_ReclaimPlugin.Localize(analysisContext.Item.m_shared.m_name)} " +
                                         $"for {analysisContext.RecyclingImpediments.Count} reasons:");
                foreach (var impediment in analysisContext.RecyclingImpediments) stringBuilder.AppendLine(impediment);
            }

            if (stringBuilder.Length == 0 || Recycle_N_ReclaimPlugin.NotifyOnSalvagingImpediments.Value == Recycle_N_ReclaimPlugin.Toggle.Off) return;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, stringBuilder.ToString());
        }

        public static List<RecyclingAnalysisContext> GetRecyclingAnalysisForInventory(Inventory inventory, Player player)
        {
            var itemListSnapshot = new List<ItemDrop.ItemData>();
            // copy the inventory, otherwise collection will constantly change causing issues
            itemListSnapshot.AddRange(inventory.GetAllItems());
            var analysisList = new List<RecyclingAnalysisContext>();
            for (var index = 0; index < itemListSnapshot.Count; index++)
            {
                var item = itemListSnapshot[index];
                var analysisContext = new RecyclingAnalysisContext(item);
                analysisList.Add(analysisContext);
                TryAnalyzeOneItem(analysisContext, inventory, player);
            }

            return analysisList;
        }

        private static void RecycleOneItemInInventory(RecyclingAnalysisContext analysisContext, Inventory inventory,
            Player player)
        {
            if (!TryAnalyzeOneItem(analysisContext, inventory, player)) return;

            if (analysisContext.RecyclingImpediments.Count > 0)
                return;
            DoInventoryChanges(analysisContext, inventory, player);
        }

        public static bool TryAnalyzeOneItem(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
        {
            if (!TryFindRecipeForItem(analysisContext, player)) return false;
            AnalyzeCraftingStationRequirements(analysisContext, player);
            //todo: optimize two .GetComponent<ItemDrop> calls 
            AnalyzeMaterialYieldForItem(analysisContext);
            AnalyzeInventoryHasEnoughEmptySlots(analysisContext, inventory);
            AnalyzeItemDisplayImpediments(analysisContext, inventory, player);
            return true;
        }

        private static void AnalyzeCraftingStationRequirements(RecyclingAnalysisContext analysisContext, Player player)
        {
            if (Recycle_N_ReclaimPlugin.RequireExactCraftingStationForRecycling.Value == Recycle_N_ReclaimPlugin.Toggle.Off) return;
            var recipeCraftingStation = analysisContext.Recipe.m_craftingStation;
            if (recipeCraftingStation == null) return;
            var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
            var item = analysisContext.Item;
            if (currentCraftingStation == null
                || currentCraftingStation.m_name != recipeCraftingStation.m_name
                || currentCraftingStation.GetLevel() < analysisContext.Item.m_quality)
            {
                analysisContext.RecyclingImpediments.Add(
                    $"Recipe requires " +
                    $"<color=orange>{Recycle_N_ReclaimPlugin.Localize(recipeCraftingStation.m_name)}</color> " +
                    $"of level <color=orange>{item.m_quality}</color>");
            }
        }


        private static void AnalyzeItemDisplayImpediments(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
        {
            if (player.GetInventory() != inventory) return;

            if (analysisContext.Item.m_equipped && Recycle_N_ReclaimPlugin.HideEquippedItemsInRecyclingTab.Value == Recycle_N_ReclaimPlugin.Toggle.On)
                analysisContext.DisplayImpediments.Add("Item is currently equipped");

            if (analysisContext.Item.m_gridPos.y == 0 && Recycle_N_ReclaimPlugin.IgnoreItemsOnHotbar.Value == Recycle_N_ReclaimPlugin.Toggle.On)
                analysisContext.DisplayImpediments.Add("Item is on hotbar and setting to ignore hotbar is set");

            if (analysisContext.Recipe.m_craftingStation?.m_name != null
                && Recycle_N_ReclaimPlugin.StationFilterEnabled.Value == Recycle_N_ReclaimPlugin.Toggle.On
                && Recycle_N_ReclaimPlugin.StationFilterList.Contains(analysisContext.Recipe.m_craftingStation.m_name))
                analysisContext.DisplayImpediments.Add(
                    $"Item is from filtered station ({Recycle_N_ReclaimPlugin.Localize(analysisContext.Recipe.m_craftingStation.m_name)})");
        }

        public static void DoInventoryChanges(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
        {
            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Inventory changes requested");
            foreach (var entry in analysisContext.Entries)
            {
                if (entry is { Amount: 0, InitialRecipeHadZero: true }) continue;
                var addedItem = inventory.AddItem(
                    entry.Prefab.name, entry.Amount, entry.mQuality,
                    entry.mVariant, player.GetPlayerID(), player.GetPlayerName()
                );
                if (addedItem != null)
                {
                    Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Added {entry.Amount} of {entry.Prefab.name}");
                    continue;
                }

                if (entry.Amount < 1 && Recycle_N_ReclaimPlugin.PreventZeroResourceYields.Value == Recycle_N_ReclaimPlugin.Toggle.Off)
                {
                    Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Adding item failed, but player disabled zero resource yields prevention, item loss expected. ");
                    continue;
                }

                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogError(
                    "Inventory refused to add item after valid analysis! Check the error from the inventory for details. Will mark analysis for dumping.");
                analysisContext.ShouldErrorDumpAnalysis = true;
                analysisContext.RecyclingImpediments.Add(
                    $"Inventory could not add item {Recycle_N_ReclaimPlugin.Localize(entry.Prefab.name)}");
            }

            if (inventory.RemoveItem(analysisContext.Item))
            {
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Removed item {analysisContext.Item.m_shared.m_name}");
                return;
            }

            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogError(
                "Inventory refused to remove item after valid analysis! Check the error from the inventory for details. Will mark analysis for dumping.");
            analysisContext.ShouldErrorDumpAnalysis = true;
            analysisContext.RecyclingImpediments.Add($"Inventory could not remove item {Recycle_N_ReclaimPlugin.Localize(analysisContext.Item.m_shared.m_name)}");
        }

        private static bool TryFindRecipeForItem(RecyclingAnalysisContext analysisContext, Player player)
        {
            var item = analysisContext.Item;
            var foundRecipes = ObjectDB.instance.m_recipes
                // some recipes are just weird, so check for null item, data and even shared (somehow it happens)
                .Where(rec => rec?.m_item?.m_itemData?.m_shared?.m_name == item.m_shared.m_name)
                .ToList();
            if (foundRecipes.Count == 0)
            {
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Could not find a recipe for {item.m_shared.m_name}");
                analysisContext.DisplayImpediments.Add($"Could not find a recipe for {item.m_shared.m_name}");
                analysisContext.Recipe = null;
                return false;
            }

            /*if (foundRecipes.Count > 1)
            {
                //todo: handle multi recipe thing, rework later
                foundRecipes = foundRecipes.OrderBy(r => r.m_amount).Take(1).ToList();
            }*/
            if (foundRecipes.Count > 1) // Attempt to complete the task above.
            {
                /* How this one works:
                 * 1. Get all recipes that the player knows.
                 * 2. If the player knows any of the recipes, prioritize those.
                 * 3. If the player doesn't know any of the recipes, prioritize the one with the smallest amount.
                 */


                // handle multi recipe thing
                var knownRecipes = foundRecipes.Where(r => player.IsRecipeKnown(r.m_item.m_itemData.m_shared.m_name)).ToList();
                if (knownRecipes.Any())
                {
                    // prioritize known recipes
                    foundRecipes = knownRecipes;
                }

                // still select the one with the smallest amount if multiple options exist
                foundRecipes = foundRecipes.OrderBy(r => r.m_amount).Take(1).ToList();
            }

            analysisContext.Recipe = foundRecipes.FirstOrDefault();
            if (!player.IsRecipeKnown(analysisContext.Recipe.m_item.m_itemData.m_shared.m_name) &&
                Recycle_N_ReclaimPlugin.AllowRecyclingUnknownRecipes.Value == Recycle_N_ReclaimPlugin.Toggle.Off)
            {
                analysisContext.RecyclingImpediments.Add($"Recipe for {Localization.instance.Localize(item.m_shared.m_name)} not known.");
            }

            return true;
        }

        private static void AnalyzeInventoryHasEnoughEmptySlots(RecyclingAnalysisContext analysisContext,
            Inventory inventory)
        {
            var emptySlotsAmount = inventory.GetEmptySlots();
            var needsSlots = analysisContext.Entries.Sum(entry =>
                Math.Ceiling(entry.Amount /
                             (double)entry.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize));

            if (emptySlotsAmount >= needsSlots) return;
            var message = emptySlotsAmount == 0 ? "none" : "only" + emptySlotsAmount;
            analysisContext.RecyclingImpediments.Add($"Need {needsSlots} slots but {message} were available");
        }

        private static void AnalyzeMaterialYieldForItem(RecyclingAnalysisContext analysisContext)
        {
            var recyclingRate = Recycle_N_ReclaimPlugin.RecyclingRate.Value;
            var itemData = analysisContext.Item;
            var recipe = analysisContext.Recipe;
            // Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Gathering recycling result for {itemData.m_shared.m_name}");
            var amountToCraftedRecipeAmountPercentage = itemData.m_stack / (double)recipe.m_amount;
            foreach (var resource in recipe.m_resources)
            {
                var rItemData = resource.m_resItem.m_itemData;
                var preFab = ObjectDB.instance.m_items.FirstOrDefault(item =>
                    item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == rItemData.m_shared.m_name);
                if (preFab == null)
                {
                    Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogWarning($"Could not find a prefab for {itemData.m_shared.m_name}! Won't be able to spawn items. You might want to report this!");
                    analysisContext.RecyclingImpediments.Add($"Could not find item {Localization.instance.Localize(itemData.m_shared.m_name)}({itemData.m_shared.m_name})");
                    continue;
                }

                var (finalAmount, initialRecipeHadZero) = CalculateFinalAmount(itemData, resource, amountToCraftedRecipeAmountPercentage,
                    recyclingRate);
                analysisContext.Entries.Add(new RecyclingAnalysisContext.ReclaimingYieldEntry(preFab, rItemData, finalAmount, rItemData.m_quality, rItemData.m_variant, initialRecipeHadZero));
                if (Jewelcrafting.API.IsLoaded())
                {
                    CheckJewelCrafting(analysisContext);
                }

                if (Recycle_N_ReclaimPlugin.PreventZeroResourceYields.Value == Recycle_N_ReclaimPlugin.Toggle.On && finalAmount == 0 && !initialRecipeHadZero)
                {
                    analysisContext.RecyclingImpediments.Add($"Recycling would yield 0 of {Localization.instance.Localize(resource.m_resItem.m_itemData.m_shared.m_name)}");
                }
            }
        }

        private static void CheckJewelCrafting(RecyclingAnalysisContext recyclingAnalysisContext)
        {
            // TODO: It's seeming to always return one more of the gems than it's supposed to. I'm not sure why. I'll have to look into it later.
            var itemData = recyclingAnalysisContext.Item;
            if (Jewelcrafting.API.GetGems(itemData).Any())
            {
                var gemsOnItem = Jewelcrafting.API.GetGems(itemData);

                Dictionary<ItemDrop, ItemDrop.ItemData> gemItemData = gemsOnItem
                    .Where(gem => gem != null)
                    .Select(gem => ObjectDB.instance.GetItemPrefab(gem.gemPrefab).GetComponent<ItemDrop>())
                    .Where(itemDrop => itemDrop != null)
                    .ToDictionary(itemDrop => itemDrop, itemDrop => itemDrop.m_itemData);

                foreach (var gemItem in gemItemData)
                {
                    bool recipeCheck = ObjectDB.instance.GetRecipe(gemItem.Value) && !Player.m_localPlayer.IsRecipeKnown(gemItem.Value.m_shared.m_name);
                    bool knownMaterialCheck = !Player.m_localPlayer.m_knownMaterial.Contains(gemItem.Value.m_shared.m_name);

                    if (Recycle_N_ReclaimPlugin.returnUnknownResources.Value == Recycle_N_ReclaimPlugin.Toggle.Off && (recipeCheck || knownMaterialCheck))
                    {
                        recyclingAnalysisContext.RecyclingImpediments.Add($"Recipe for {Localization.instance.Localize(gemItem.Value.m_shared.m_name)} not known.");
                        return;
                    }

                    recyclingAnalysisContext.Entries.Add(
                        new RecyclingAnalysisContext.ReclaimingYieldEntry(gemItem.Key.gameObject, gemItem.Value, ObjectDB.instance.GetRecipe(gemItem.Value).m_amount,
                            gemItem.Value.m_quality, gemItem.Value.m_variant, false));
                }
            }
        }

        private static Result CalculateFinalAmount(ItemDrop.ItemData itemData, Piece.Requirement resource,
            double amountToCraftedRecipeAmountPercentage, float recyclingRate)
        {
            var result = new Result();

            var amountPerLevelSum = Enumerable.Range(1, itemData.m_quality)
                .Select(resource.GetAmount)
                .Sum();

            if (amountPerLevelSum == 0)
            {
                result.Amount = 0;
                result.InitialRecipeHadZero = true;
                return result;
            }

            var stackCompensated = amountPerLevelSum * amountToCraftedRecipeAmountPercentage;
            var realAmount = Math.Floor(stackCompensated * recyclingRate);
            result.Amount = (int)realAmount;

            if (realAmount < 1 && itemData.m_shared.m_maxStackSize == 1
                               && Recycle_N_ReclaimPlugin.UnstackableItemsAlwaysReturnAtLeastOneResource.Value == Recycle_N_ReclaimPlugin.Toggle.On)
            {
                result.Amount = 1;
            }

            if (Recycle_N_ReclaimPlugin.DebugAllowSpammyLogs.Value == Recycle_N_ReclaimPlugin.Toggle.On)
            {
                Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Calculations report.\n" +
                                                                         $" = = = {resource.m_resItem.m_itemData.m_shared.m_name} - " +
                                                                         $"REA:{resource.m_amount} APLS: {amountPerLevelSum} IQ:{itemData.m_quality} " +
                                                                         $"STK:{itemData.m_stack}({itemData.m_shared.m_maxStackSize}) SC:{stackCompensated} " +
                                                                         $"ATCRAP:{amountToCraftedRecipeAmountPercentage} A:{amountPerLevelSum}, " +
                                                                         $"RA:{realAmount} FA:{result.Amount}");
            }

            result.InitialRecipeHadZero = false;
            return result;
        }

        private class Result
        {
            public int Amount { get; set; }
            public bool InitialRecipeHadZero { get; set; }

            public void Deconstruct(out int amount, out bool initialRecipeHadZero)
            {
                amount = Amount;
                initialRecipeHadZero = InitialRecipeHadZero;
            }
        }
    }
}