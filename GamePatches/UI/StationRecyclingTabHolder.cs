﻿using System.Collections.Generic;
using System.Linq;
using Recycle_N_Reclaim.GamePatches.Recycling;
using UnityEngine;
using UnityEngine.UI;

namespace Recycle_N_Reclaim.GamePatches.UI
{
    public class StationRecyclingTabHolder : MonoBehaviour
    {
        internal static GameObject _recyclingTabButtonGameObject;
        private Button _recyclingTabButtonComponent;
        private List<RecyclingAnalysisContext> _recyclingAnalysisContexts = new();

        private void Start()
        {
            InvokeRepeating(nameof(EnsureRecyclingTabExists), 5f, 5f);
        }

        void EnsureRecyclingTabExists()
        {
            if (InventoryGui.instance == null) return;
            if (_recyclingTabButtonComponent == null) SetupTabButton();
        }

        private void OnDestroy()
        {
            try
            {
                Destroy(_recyclingTabButtonGameObject.gameObject);
            }
            catch
            {
                // ignored
            }
        }

        internal void SetupTabButton()
        {
            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Creating tab button");
            var upgradeTabTransform = InventoryGui.instance.m_tabUpgrade.transform;
            _recyclingTabButtonGameObject = Instantiate(InventoryGui.instance.m_tabUpgrade.gameObject,
                upgradeTabTransform.position, upgradeTabTransform.rotation, upgradeTabTransform.parent);
            _recyclingTabButtonGameObject.name = "RECLAIM";
            // Unity3d is inconsistent and for whatever reason game object order in the parent transform
            // matters for the UI components 😐
            _recyclingTabButtonGameObject.transform.parent.Find("TabBorder").SetAsLastSibling();
            _recyclingTabButtonGameObject.transform.localPosition = new Vector3(-45, -94, 0);
            var textComponent = _recyclingTabButtonGameObject.GetComponentInChildren<Text>();
            textComponent.text = "RECLAIM";

            _recyclingTabButtonComponent = _recyclingTabButtonGameObject.GetComponent<Button>();
            _recyclingTabButtonComponent.interactable = true;
            _recyclingTabButtonComponent.onClick = new Button.ButtonClickedEvent();
            _recyclingTabButtonComponent.onClick.AddListener(OnRecycleClick);
            bool shouldBeActive = Player.m_localPlayer?.GetCurrentCraftingStation() != null;
            _recyclingTabButtonGameObject.SetActive(shouldBeActive);
        }

        private void OnRecycleClick()
        {
            _recyclingTabButtonComponent.interactable = false;
            InventoryGui.instance.m_tabCraft.interactable = true;
            InventoryGui.instance.m_tabUpgrade.interactable = true;
            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("OnRecycleClick");
            UpdateCraftingPanel();
        }

        public void UpdateCraftingPanel()
        {
            var igui = InventoryGui.instance;
            var localPlayer = Player.m_localPlayer;
            if (localPlayer.GetCurrentCraftingStation() == null && localPlayer.NoCostCheat() == false)
            {
                igui.m_tabCraft.interactable = false;
                igui.m_tabUpgrade.interactable = true;
                igui.m_tabUpgrade.gameObject.SetActive(false);

                _recyclingTabButtonComponent.interactable = true;
                _recyclingTabButtonComponent.gameObject.SetActive(false);
            }
            else
                igui.m_tabUpgrade.gameObject.SetActive(true);

            UpdateRecyclingList();

            if (igui.get_m_availableRecipes().Count > 0)
            {
                igui.SetRecipe(igui.get_m_selectedRecipe().Key != null ? igui.GetSelectedRecipeIndex() : 0, true);
            }
            else
                igui.SetRecipe(-1, true);
        }

        public void UpdateRecyclingList()
        {
            var localPlayer = Player.m_localPlayer;
            var igui = InventoryGui.instance;
            igui.get_m_availableRecipes().Clear();
            var m_recipeList = igui.get_m_recipeList();
            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Old recipe list had {m_recipeList.Count} entries. Cleaning up");
            foreach (var recipeElement in m_recipeList) Destroy(recipeElement);
            m_recipeList.Clear();

            _recyclingAnalysisContexts.Clear();
            var validRecycles = Reclaimer.GetRecyclingAnalysisForInventory(localPlayer.GetInventory(), localPlayer)
                .Where(context => context.Recipe != null
                                  // we want to reply on display impediments mainly,
                                  // but null recipes are really a deal breaker and
                                  // require to many checks and workarounds
                                  // so it makes more sense to just filter them out completely 
                                  && context.DisplayImpediments.Count == 0);
            _recyclingAnalysisContexts.AddRange(validRecycles);
            foreach (var context in _recyclingAnalysisContexts)
            {
                if (context.Recipe == null) continue;
                AddRecipeToList(context, m_recipeList);
            }

            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"Added {m_recipeList.Count} entries");

            igui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(igui.get_m_recipeListBaseSize(), m_recipeList.Count * igui.m_recipeListSpace));
        }

        private void AddRecipeToList(RecyclingAnalysisContext context, List<GameObject> m_recipeList)
        {
            var count = m_recipeList.Count;

            var igui = InventoryGui.instance;
            var m_recipeListRoot = igui.m_recipeListRoot;
            var element = Instantiate(igui.m_recipeElementPrefab, m_recipeListRoot);
            element.SetActive(true);
            ((RectTransform)element.transform).anchoredPosition = new Vector2(0.0f, count * -igui.m_recipeListSpace);
            var component1 = element.transform.Find("icon").GetComponent<Image>();
            component1.sprite = context.Item.GetIcon();
            component1.color = context.RecyclingImpediments.Count == 0 ? Color.white : new Color(1f, 0.0f, 1f, 0.0f);
            var component2 = element.transform.Find("name").GetComponent<Text>();
            var str = Localization.instance.Localize(context.Item.m_shared.m_name);
            if (context.Item.m_stack > 1 && context.Item.m_shared.m_maxStackSize > 1)
                str = str + " x" + context.Item.m_stack;
            component2.text = str;
            component2.color = context.RecyclingImpediments.Count == 0 ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f);
            var component3 = element.transform.Find("Durability").GetComponent<GuiBar>();
            if (context.Item.m_shared.m_useDurability &&
                context.Item.m_durability < (double)context.Item.GetMaxDurability())
            {
                component3.gameObject.SetActive(true);
                component3.SetValue(context.Item.GetDurabilityPercentage());
            }
            else
                component3.gameObject.SetActive(false);

            var component4 = element.transform.Find("QualityLevel").GetComponent<Text>();

            component4.gameObject.SetActive(true);
            component4.text = context.Item.m_quality.ToString();

            element.GetComponent<Button>().onClick.AddListener(() => igui.OnSelectedRecipe(element));
            m_recipeList.Add(element);
            igui.get_m_availableRecipes()
                .Add(new KeyValuePair<Recipe, ItemDrop.ItemData>(context.Recipe, context.Item));
        }


        public bool InRecycleTab()
        {
            if(Recycle_N_ReclaimPlugin.HasAuga)
            {
                return Recycle_N_ReclaimPlugin.IsRecycleTabActiveAuga();
            }
            if (_recyclingTabButtonComponent == null) return false;
            return !_recyclingTabButtonComponent.interactable;
        }

        public void SetInteractable(bool interactable)
        {
            _recyclingTabButtonComponent.interactable = interactable;
        }

        public void SetActive(bool active)
        {
            if (Recycle_N_ReclaimPlugin.HasAuga)
            {
                return;
            }

            if (Recycle_N_ReclaimPlugin.EnableExperimentalCraftingTabUI.Value == Recycle_N_ReclaimPlugin.Toggle.Off) return;

            _recyclingTabButtonGameObject.SetActive(active);
        }

        public void UpdateRecipe(Player player, float dt)
        {
            var igui = InventoryGui.instance;
            var selectedRecipeIndex = igui.GetSelectedRecipeIndex();

            UpdateRecyclingAnalysisContexts(selectedRecipeIndex, player);
            UpdateCraftingStationUI(player);

            if (igui.get_m_selectedRecipe().Key)
            {
                UpdateRecipeUI(selectedRecipeIndex, igui);
            }
            else
            {
                ClearRecipeUI(igui);
            }

            UpdateCraftingTimer(dt, selectedRecipeIndex, player, igui);
        }

        private void UpdateRecyclingAnalysisContexts(int selectedRecipeIndex, Player player)
        {
            if (selectedRecipeIndex > -1 && _recyclingAnalysisContexts.Count > 0 && selectedRecipeIndex < _recyclingAnalysisContexts.Count)
            {
                var context = _recyclingAnalysisContexts[selectedRecipeIndex];
                var newContext = new RecyclingAnalysisContext(context.Item);
                Reclaimer.TryAnalyzeOneItem(newContext, player.GetInventory(), player);
                _recyclingAnalysisContexts[selectedRecipeIndex] = newContext;
            }
        }

        private void UpdateCraftingStationUI(Player player)
        {
            var igui = InventoryGui.instance;
            var currentCraftingStation = player.GetCurrentCraftingStation();

            if (currentCraftingStation)
            {
                SetActive(igui.m_craftingStationIcon.gameObject, true);
                SetActive(igui.m_craftingStationLevelRoot.gameObject, true);
                igui.m_craftingStationName.text = Localization.instance.Localize(currentCraftingStation.m_name);
                igui.m_craftingStationIcon.sprite = currentCraftingStation.m_icon;
                igui.m_craftingStationLevel.text = currentCraftingStation.GetLevel().ToString();
            }
            else
            {
                SetActive(igui.m_craftingStationIcon.gameObject, false);
                SetActive(igui.m_craftingStationLevelRoot.gameObject, false);
                igui.m_craftingStationName.text = Localization.instance.Localize("$hud_crafting");
            }
        }

        private void UpdateRecipeUI(int selectedRecipeIndex, InventoryGui igui)
        {
            var analysisContext = _recyclingAnalysisContexts[selectedRecipeIndex];
            var itemData = igui.get_m_selectedRecipe().Value;
            var num = itemData?.m_quality + 1 ?? 1;

            igui.m_recipeIcon.enabled = true;
            igui.m_recipeName.enabled = true;
            igui.m_recipeDecription.enabled = true;

            igui.m_recipeIcon.sprite = igui.get_m_selectedRecipe().Key.m_item.m_itemData.m_shared.m_icons[itemData?.m_variant ?? igui.get_m_selectedVariant()];
            string str = Localization.instance.Localize(igui.get_m_selectedRecipe().Key.m_item.m_itemData.m_shared.m_name);
            if (analysisContext.Item.m_stack > 1)
                str = str + " x" + analysisContext.Item.m_stack;
            igui.m_recipeName.text = str;

            if (analysisContext.RecyclingImpediments.Count == 0)
                igui.m_recipeDecription.text = "\nAll requirements are <color=orange>fulfilled</color>";
            else
                igui.m_recipeDecription.text = "\nRecycling blocked for these reasons:\n\n<size=15>" + $"{string.Join("\n", analysisContext.RecyclingImpediments)}" + "</size>";

            if (itemData != null)
            {
                SetActive(igui.m_itemCraftType.gameObject, true);
                igui.m_itemCraftType.text = Localization.instance.Localize($"Recycle {itemData.m_shared.m_name} of quality {itemData.m_quality}");
            }
            else
                SetActive(igui.m_itemCraftType.gameObject, false);

            SetActive(igui.m_variantButton.gameObject, igui.get_m_selectedRecipe().Key.m_item.m_itemData.m_shared.m_variants > 1 && igui.get_m_selectedRecipe().Value == null);

            if (Recycle_N_ReclaimPlugin.epicLootAssembly == null)
            {
                SetupRequirementList(analysisContext);
            }
            else
            {
                SetupRequirementListEpicLoot(analysisContext);
            }

            SetActive(igui.m_minStationLevelIcon.gameObject, false);
            igui.m_craftButton.interactable = analysisContext.RecyclingImpediments.Count == 0;
            igui.m_craftButton.GetComponentInChildren<Text>().text = "Reclaim";
            igui.m_craftButton.GetComponent<UITooltip>().m_text = analysisContext.RecyclingImpediments.Count == 0 ? "" : Localization.instance.Localize("$msg_missingrequirement");
        }

        private void ClearRecipeUI(InventoryGui igui)
        {
            igui.m_recipeIcon.enabled = false;
            igui.m_recipeName.enabled = false;
            igui.m_recipeDecription.enabled = false;

            SetActive(igui.m_qualityPanel.gameObject, false);
            SetActive(igui.m_minStationLevelIcon.gameObject, false);
            igui.m_craftButton.GetComponent<UITooltip>().m_text = "";
            SetActive(igui.m_variantButton.gameObject, false);

            igui.m_craftButton.GetComponentInChildren<Text>().text = "Reclaim";
            SetActive(igui.m_itemCraftType.gameObject, false);
            for (int index = 0; index < igui.m_recipeRequirementList.Length; ++index)
                InventoryGui.HideRequirement(igui.m_recipeRequirementList[index].transform);
            igui.m_craftButton.interactable = false;
        }

        private void UpdateCraftingTimer(float dt, int selectedRecipeIndex, Player player, InventoryGui igui)
        {
            if (igui.get_m_craftTimer() < 0.0)
            {
                SetActive(igui.m_craftProgressPanel.gameObject, false);
                SetActive(igui.m_craftButton.gameObject, true);
            }
            else
            {
                SetActive(igui.m_craftButton.gameObject, false);
                SetActive(igui.m_craftProgressPanel.gameObject, true);
                igui.m_craftProgressBar.SetMaxValue(igui.m_craftDuration);
                igui.m_craftProgressBar.SetValue(igui.get_m_craftTimer());
                igui.set_m_craftTimer(igui.get_m_craftTimer() + dt);
                if (igui.get_m_craftTimer() >= (double)igui.m_craftDuration)
                {
                    Reclaimer.DoInventoryChanges(_recyclingAnalysisContexts[selectedRecipeIndex], player.GetInventory(), player);
                    igui.set_m_craftTimer(-1f);
                    igui.SetRecipe(-1, false);
                    UpdateCraftingPanel();
                }
            }
        }

        private void SetActive(GameObject gameObject, bool isActive)
        {
            if (gameObject)
            {
                gameObject.SetActive(isActive);
            }
        }

        /* TLDR; For some reason, EpicLoot doesn't like the way the vanilla game handles the recipe requirements UI
         or at least the way I have to do it below, so we have to stick to my older method...which is compatible with EpicLoot. */
        private void SetupRequirementListEpicLoot(RecyclingAnalysisContext analysisContexts)
        {
            var igui = InventoryGui.instance;

            var filteredEntries = analysisContexts.Entries.Where(entry => entry.Amount != 0).ToList(); // Filter before the loop to avoid unnecessary iterations

            for (int i = 0; i < igui.m_recipeRequirementList.Length; ++i)
            {
                var elementTransform = igui.m_recipeRequirementList[i].transform;
                if (i < filteredEntries.Count)
                {
                    SetupRequirementEpicLoot(elementTransform, filteredEntries[i]);
                }
                else
                {
                    InventoryGui.HideRequirement(elementTransform);
                }
            }
        }

        public static void SetupRequirementEpicLoot(Transform elementRoot,
            RecyclingAnalysisContext.ReclaimingYieldEntry entry)
        {
            var component1 = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            var component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
            var component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            var component4 = elementRoot.GetComponent<UITooltip>();
            component1.gameObject.SetActive(true);
            component2.gameObject.SetActive(true);
            component3.gameObject.SetActive(true);
            component1.sprite = entry.RecipeItemData.GetIcon();
            component1.color = Color.white;
            component4.m_text = Localization.instance.Localize(entry.RecipeItemData.m_shared.m_name);
            component2.text = Localization.instance.Localize(entry.RecipeItemData.m_shared.m_name);
            component3.text = entry.Amount.ToString();
            component3.color = Color.white;
        }

        private void SetupRequirementList(RecyclingAnalysisContext analysisContexts)
        {
            var igui = InventoryGui.instance;
            int index1 = 0;
            var filteredEntries = analysisContexts.Entries.Where(entry => entry.Amount != 0).ToList(); // Filter before the loop to avoid unnecessary iterations

            int num = 0;
            if (filteredEntries.Count > 4)
                num = (int)Time.fixedTime % (int)Mathf.Ceil(filteredEntries.Count / (float)igui.m_recipeRequirementList.Length) * igui.m_recipeRequirementList.Length;

            for (int index2 = num; index2 < filteredEntries.Count; ++index2)
            {
                if (SetupRequirement(igui.m_recipeRequirementList[index1].transform, filteredEntries[index2]))
                    ++index1;
                if (index1 >= igui.m_recipeRequirementList.Length)
                    break;
            }

            for (; index1 < igui.m_recipeRequirementList.Length; ++index1)
                InventoryGui.HideRequirement(igui.m_recipeRequirementList[index1].transform);
        }

        public static bool SetupRequirement(Transform elementRoot,
            RecyclingAnalysisContext.ReclaimingYieldEntry entry)
        {
            var component1 = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            var component2 = elementRoot.transform.Find("res_name").GetComponent<Text>();
            var component3 = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            var component4 = elementRoot.GetComponent<UITooltip>();
            component1.gameObject.SetActive(true);
            component2.gameObject.SetActive(true);
            component3.gameObject.SetActive(true);
            component1.sprite = entry.RecipeItemData.GetIcon();
            component1.color = Color.white;
            component4.m_text = Localization.instance.Localize(entry.RecipeItemData.m_shared.m_name);
            component2.text = Localization.instance.Localize(entry.RecipeItemData.m_shared.m_name);
            component3.text = entry.Amount.ToString();
            component3.color = Color.white;
            if (entry.Amount <= 0)
            {
                InventoryGui.HideRequirement(elementRoot);
                return false;
            }

            return true;
        }
    }
}