namespace Recycle_N_Reclaim.GamePatches.Recycling;

public static class Reclaimer
{
    private static bool _loggedErrorsOnce = false;

    public static void RecycleInventoryForAllRecipes(Inventory inventory, string containerName, Player player)
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
            // log the container name and prefab name
            var prefabName = global::Utils.GetPrefabName(item.m_dropPrefab);
            Recycle_N_ReclaimLogger.LogDebug($"containerName: {containerName}, prefabName: {prefabName}");
            if (!GroupUtils.IsPrefabExcludedInContainer(containerName, prefabName))
            {
                RecycleOneItemInInventory(analysisContext, inventory, player);
            }

            if (analysisContext.ShouldErrorDumpAnalysis || DebugAlwaysDumpAnalysisContext.Value.IsOn())
            {
                analysisContext.Dump();
            }
        }

        var stringBuilder = new StringBuilder();
        foreach (var analysisContext in analysisList.Where(analysis => analysis.RecyclingImpediments.Count > 0))
        {
            stringBuilder.AppendLine(Localize("$azumatt_recycle_n_reclaim_could_not_recycle", Localize(analysisContext.Item.m_shared.m_name), analysisContext.RecyclingImpediments.Count.ToString()));

            foreach (var impediment in analysisContext.RecyclingImpediments) stringBuilder.AppendLine(impediment);
        }

        if (stringBuilder.Length == 0 || NotifyOnSalvagingImpediments.Value.IsOff()) return;
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

    private static void RecycleOneItemInInventory(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
    {
        if (!TryAnalyzeOneItem(analysisContext, inventory, player)) return;

        if (analysisContext.RecyclingImpediments.Count > 0)
            return;
        DoInventoryChanges(analysisContext, inventory, player);
    }

    public static bool TryAnalyzeOneItem(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player, bool fromContainer = false)
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
        if (RequireExactCraftingStationForRecycling.Value.IsOff()) return;
        var recipeCraftingStation = analysisContext.Recipe.m_craftingStation;
        if (recipeCraftingStation == null) return;
        var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
        var item = analysisContext.Item;
        if (currentCraftingStation == null || currentCraftingStation.m_name != recipeCraftingStation.m_name || currentCraftingStation.GetLevel() < analysisContext.Item.m_quality)
        {
            analysisContext.RecyclingImpediments.Add(Localize("$azumatt_recycle_n_reclaim_recipe_requires", Localize(recipeCraftingStation.m_name), item.m_quality.ToString()));
        }
    }


    private static void AnalyzeItemDisplayImpediments(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
    {
        if (player.GetInventory() != inventory) return;

        if (analysisContext.Item.m_equipped && HideEquippedItemsInRecyclingTab.Value.IsOn())
            analysisContext.DisplayImpediments.Add(Localize("$azumatt_recycle_n_reclaim_item_equipped"));

        if (analysisContext.Item.m_gridPos.y == 0 && IgnoreItemsOnHotbar.Value.IsOn())
            analysisContext.DisplayImpediments.Add(Localize("$azumatt_recycle_n_reclaim_item_on_hotbar"));

        if (analysisContext.Recipe.m_craftingStation?.m_name != null
            && StationFilterEnabled.Value.IsOn()
            && StationFilterList.Contains(global::Utils.GetPrefabName(analysisContext.Recipe.m_craftingStation.transform.root.gameObject)))
            analysisContext.DisplayImpediments.Add(Localize("$azumatt_recycle_n_reclaim_item_from_station", Localize(analysisContext.Recipe.m_craftingStation.m_name)));
    }

    public static void DoInventoryChanges(RecyclingAnalysisContext analysisContext, Inventory inventory, Player player)
    {
        Recycle_N_ReclaimLogger.LogDebug("Inventory changes requested");
        foreach (var entry in analysisContext.Entries)
        {
            if (entry is { Amount: 0, InitialRecipeHadZero: true }) continue;
            ItemDrop.ItemData? addedItem = null;
            addedItem = ApplyCraftedBy.Value.IsOn() 
                ? inventory.AddItem(entry.Prefab.name, entry.Amount, entry.mQuality, entry.mVariant, player.GetPlayerID(), player.GetPlayerName()) 
                : inventory.AddItem(entry.Prefab.name, entry.Amount, entry.mQuality, entry.mVariant, 0, "");

            if (addedItem != null)
            {
                Recycle_N_ReclaimLogger.LogDebug($"Added {entry.Amount} of {entry.Prefab.name}");
                continue;
            }

            if (entry.Amount < 1 && PreventZeroResourceYields.Value.IsOff())
            {
                Recycle_N_ReclaimLogger.LogDebug("Adding item failed, but player disabled zero resource yields prevention, item loss expected. ");
                continue;
            }

            Recycle_N_ReclaimLogger.LogError("Inventory refused to add item after valid analysis! Check the error from the inventory for details. Will mark analysis for dumping.");
            analysisContext.ShouldErrorDumpAnalysis = true;
            analysisContext.RecyclingImpediments.Add(Localize("$azumatt_recycle_n_reclaim_inventory_couldnt_add", Localize(entry.Prefab.name)));
            if (analysisContext.ShouldErrorDumpAnalysis || DebugAlwaysDumpAnalysisContext.Value.IsOn())
            {
                analysisContext.Dump();
            }
        }

        if (inventory.RemoveItem(analysisContext.Item))
        {
            Recycle_N_ReclaimLogger.LogDebug($"Removed item {analysisContext.Item.m_shared.m_name}");
            return;
        }

        Recycle_N_ReclaimLogger.LogError("Inventory refused to remove item after valid analysis! Check the error from the inventory for details. Will mark analysis for dumping.");
        analysisContext.ShouldErrorDumpAnalysis = true;
        analysisContext.RecyclingImpediments.Add(Localize("$azumatt_recycle_n_reclaim_inventory_couldnt_remove", Localize(analysisContext.Item.m_shared.m_name)));
        if (analysisContext.ShouldErrorDumpAnalysis || DebugAlwaysDumpAnalysisContext.Value.IsOn())
        {
            analysisContext.Dump();
        }
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
            Recycle_N_ReclaimLogger.LogDebug($"Could not find a recipe for {item.m_shared.m_name}");
            analysisContext.DisplayImpediments.Add(Localize("$azumatt_recycle_n_reclaim_couldnt_find_recipe",
                Localize(item.m_shared.m_name)));
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

        analysisContext.Recipe = foundRecipes.FirstOrDefault()!;
        if (!player.IsRecipeKnown(analysisContext.Recipe.m_item.m_itemData.m_shared.m_name) && AllowRecyclingUnknownRecipes.Value.IsOff())
        {
            var localizedString = Localize("$azumatt_recycle_n_reclaim_recipe_not_known", Localize(item.m_shared.m_name));
            analysisContext.RecyclingImpediments.Add(localizedString);
        }

        return true;
    }

    private static void AnalyzeInventoryHasEnoughEmptySlots(RecyclingAnalysisContext analysisContext, Inventory inventory)
    {
        // based on assumption FindFreeStackSpace and FindEmptySlot are properly overridden
        bool haveEnoughSlots = true;
        int emptySlots = 0;
        double neededSlots = 0;
        foreach (var entry in analysisContext.Entries)
        {
            ItemDrop.ItemData item = entry.Prefab.GetComponent<ItemDrop>().m_itemData;
            neededSlots += Math.Ceiling(entry.Amount / (double)item.m_shared.m_maxStackSize);
            if (inventory.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel) < entry.Amount)
            {
                if (inventory.FindEmptySlot(inventory.TopFirst(item)).x == -1)
                {
                    haveEnoughSlots = false;
                    break;
                }
                else
                {
                    emptySlots++;
                }
            }
        }

        if (haveEnoughSlots) return;
        string message = Localize("$azumatt_recycle_n_reclaim_not_enough_slots", neededSlots.ToString(), emptySlots.ToString());
        analysisContext.RecyclingImpediments.Add(message);
    }


    private static void AnalyzeMaterialYieldForItem(RecyclingAnalysisContext analysisContext)
    {
        var recyclingRate = RecyclingRate.Value;
        var itemData = analysisContext.Item;
        var recipe = analysisContext.Recipe;

        var amountToCraftedRecipeAmountPercentage = itemData.m_stack / (double)recipe.m_amount;

        foreach (var resource in recipe.m_resources)
        {
            var rItemData = resource.m_resItem.m_itemData;
            if (resource.m_resItem == null)
            {
                continue;
            }

            if (rItemData == null)
            {
                continue;
            }


            var preFab = ObjectDB.instance.m_items.FirstOrDefault(item =>
                item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == rItemData.m_shared.m_name);

            if (preFab == null)
            {
                Recycle_N_ReclaimLogger.LogWarning($"Could not find a prefab for {itemData.m_shared.m_name}! Won't be able to spawn items. You might want to report this!");
                analysisContext.RecyclingImpediments.Add(Localize("$azumatt_recycle_n_reclaim_coundnt_find_item",
                    Localize(itemData.m_shared.m_name),
                    itemData.m_shared.m_name));
                continue;
            }

            var (finalAmount, initialRecipeHadZero) = CalculateFinalAmount(itemData, resource, amountToCraftedRecipeAmountPercentage,
                recyclingRate);

            if (!GroupUtils.IsPrefabExcludedInReclaiming(global::Utils.GetPrefabName(preFab)))
            {
                analysisContext.Entries.Add(new RecyclingAnalysisContext.ReclaimingYieldEntry(preFab, rItemData, finalAmount, rItemData.m_quality, rItemData.m_variant, initialRecipeHadZero));
            }

            if (PreventZeroResourceYields.Value.IsOn() && finalAmount == 0 && !initialRecipeHadZero)
            {
                analysisContext.RecyclingImpediments.Add(Localize("$azumatt_recycle_n_reclaim_recylce_yield_none",
                    Localize(resource.m_resItem.m_itemData.m_shared.m_name)));
            }
        }


        if (epicLootAssembly == null)
        {
            goto jewelcrafting;
        }

        var isMagic = false;
        var cancel = false;

        if (UpdateItemDragPatch.isMagicMethod == null)
        {
            if (!_loggedErrorsOnce)
                Recycle_N_ReclaimLogger.LogError($"EpicLoot Loaded, but missing IsMagic() Method.");

            _loggedErrorsOnce = true;
            goto jewelcrafting;
        }

        if (returnEnchantedResourcesReclaiming.Value.IsOn())
            isMagic = (bool)UpdateItemDragPatch.isMagicMethod.Invoke(null, new object[] { itemData })!;

        if (isMagic)
        {
            //Validate Existence of Method:
            if (UpdateItemDragPatch.getRarityMethod == null)
            {
                if (!_loggedErrorsOnce)
                    Recycle_N_ReclaimLogger.LogError($"EpicLoot Loaded, but missing GetRarity() Method.");
                _loggedErrorsOnce = true;

                goto jewelcrafting;
            }

            var rarity = (int)UpdateItemDragPatch.getRarityMethod.Invoke(null, new object[] { itemData })!;

            //Validate Existence of Method:
            //if (UpdateItemDragPatch.getEnchantCostsMethod == null)
            //{
            //    if (!_loggedErrorsOnce)
            //        Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogError($"EpicLoot Loaded, but missing GetEnchantCosts() Method.");

            //    _loggedErrorsOnce = true;
            //    goto jewelcrafting;
            //}

            List<KeyValuePair<ItemDrop, int>>? magicReqs =
                (List<KeyValuePair<ItemDrop, int>>)UpdateItemDragPatch.getEnchantCostsMethod?.Invoke(null, new object[] { itemData, rarity })!;

            foreach (KeyValuePair<ItemDrop, int> kvp in magicReqs)
            {
                var recipe2 = ObjectDB.instance.GetRecipe(kvp.Key.m_itemData);
                var isRecipeKnown = Player.m_localPlayer.IsRecipeKnown(kvp.Key.m_itemData.m_shared.m_name) || AllowRecyclingUnknownRecipes.Value.IsOn();
                var isKnownMaterial = Player.m_localPlayer.m_knownMaterial.Contains(kvp.Key.m_itemData.m_shared.m_name) || AllowRecyclingUnknownRecipes.Value.IsOn();

                bool canRecycle = isKnownMaterial &&
                                  (recipe2 == null ||
                                   (isRecipeKnown && AllowRecyclingUnknownRecipes.Value != Recycle_N_ReclaimPlugin.Toggle.Off));

                if (!canRecycle)
                {
                    var localizedItemName = Localize(kvp.Key.m_itemData.m_shared.m_name);
                    var localizedString = Localize("$azumatt_recycle_n_reclaim_recipe_not_known", localizedItemName ?? kvp.Key.m_itemData.m_shared.m_name);
                    analysisContext.RecyclingImpediments.Add(localizedString);
                    return;
                }

                if (!GroupUtils.IsPrefabExcludedInReclaiming(global::Utils.GetPrefabName(kvp.Key.gameObject)))
                {
                    if (recipe2 != null)
                    {
                        var yieldEntry = new RecyclingAnalysisContext.ReclaimingYieldEntry(kvp.Key.gameObject, kvp.Key.m_itemData, recipe2.m_amount,
                            kvp.Key.m_itemData.m_quality, kvp.Key.m_itemData.m_variant, false);
                        analysisContext.Entries.Add(yieldEntry);
                    }
                    else if (kvp.Key) // Magic items that do not have a recipe
                    {
                        var yieldEntry = new RecyclingAnalysisContext.ReclaimingYieldEntry(kvp.Key.gameObject, kvp.Key.m_itemData, kvp.Key.m_itemData.m_stack,
                            kvp.Key.m_itemData.m_quality, kvp.Key.m_itemData.m_variant, false);
                        analysisContext.Entries.Add(yieldEntry);
                    }
                }
            }
        }

        _loggedErrorsOnce = false; //if we get here, no errors, reset error count so that errors will print again.

        jewelcrafting:
        if (Jewelcrafting.API.IsLoaded() && returnEnchantedResourcesReclaiming.Value.IsOn())
        {
            CheckJewelCrafting(analysisContext);
        }
    }

    private static void CheckJewelCrafting(RecyclingAnalysisContext recyclingAnalysisContext)
    {
        if (recyclingAnalysisContext?.Item == null)
            return;

        var itemData = recyclingAnalysisContext.Item;
        var gemsOnItem = Jewelcrafting.API.GetGems(itemData);


        if (gemsOnItem != null && gemsOnItem.Any())
        {
            Dictionary<ItemDrop, ItemDrop.ItemData> gemItemData = gemsOnItem
                .Where(gem => gem != null)
                .Select(gem => ObjectDB.instance?.GetItemPrefab(gem.gemPrefab)?.GetComponent<ItemDrop>())
                .Where(itemDrop => itemDrop != null)
                .ToDictionary(itemDrop => itemDrop, itemDrop => itemDrop.m_itemData);

            foreach (var gemItem in gemItemData)
            {
                var recipe = ObjectDB.instance?.GetRecipe(gemItem.Value);
                var isRecipeKnown = Player.m_localPlayer.IsRecipeKnown(gemItem.Value.m_shared.m_name) || AllowRecyclingUnknownRecipes.Value.IsOn();
                var isKnownMaterial = Player.m_localPlayer.m_knownMaterial.Contains(gemItem.Value.m_shared.m_name) || AllowRecyclingUnknownRecipes.Value.IsOn();

                // Check if the item can be recycled based on your conditions
                bool canRecycle = isKnownMaterial &&
                                  (recipe == null ||
                                   (isRecipeKnown &&
                                    AllowRecyclingUnknownRecipes.Value != Recycle_N_ReclaimPlugin.Toggle.Off));

                if (!canRecycle)
                {
                    var localizedGemName = Localize(gemItem.Value.m_shared.m_name);
                    var localizedString = Localize("$azumatt_recycle_n_reclaim_recipe_not_known", localizedGemName ?? gemItem.Value.m_shared.m_name);
                    recyclingAnalysisContext.RecyclingImpediments.Add(localizedString);
                    return;
                }

                if (!GroupUtils.IsPrefabExcludedInReclaiming(global::Utils.GetPrefabName(gemItem.Key.gameObject)))
                {
                    if (recipe != null)
                    {
                        var yieldEntry = new RecyclingAnalysisContext.ReclaimingYieldEntry(gemItem.Key.gameObject, gemItem.Value, recipe.m_amount,
                            gemItem.Value.m_quality, gemItem.Value.m_variant, false);
                        recyclingAnalysisContext.Entries.Add(yieldEntry);
                    }
                    else if (gemItem.Key) // Merged gems do not have a recipe
                    {
                        var yieldEntry = new RecyclingAnalysisContext.ReclaimingYieldEntry(gemItem.Key.gameObject, gemItem.Value, 1,
                            gemItem.Value.m_quality, gemItem.Value.m_variant, false);
                        recyclingAnalysisContext.Entries.Add(yieldEntry);
                    }
                }
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
                           && UnstackableItemsAlwaysReturnAtLeastOneResource.Value.IsOn())
        {
            result.Amount = 1;
        }

        if (DebugAllowSpammyLogs.Value.IsOn())
        {
            Recycle_N_ReclaimLogger.LogDebug("Calculations report.\n" +
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