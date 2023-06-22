using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Recycle_N_Reclaim.GamePatches.Recycling;
using Recycle_N_Reclaim.GamePatches.UI;
using ServerSync;
using UnityEngine;

namespace Recycle_N_Reclaim
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Recycle_N_ReclaimPlugin : BaseUnityPlugin
    {
        internal const string ModName = "Recycle_N_Reclaim";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static Assembly epicLootAssembly;
        internal static string ConnectionError = "";
        public static StationRecyclingTabHolder RecyclingTabButtonHolder { get; private set; }
        private ContainerRecyclingButtonHolder _containerRecyclingButton;

        private readonly Harmony _harmony = new(ModGUID);
        internal static bool AdminStatus;

        public static readonly ManualLogSource Recycle_N_ReclaimLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static readonly ConfigSync ConfigSyncVar = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            // Uncomment the line below to use the LocalizationManager for localizing your mod.
            // Make sure to populate the English.yml file in the translation folder with your keys to be localized and the values associated before uncommenting!.
            //Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).
            
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSyncVar.AddLockingConfigEntry(_serverConfigLocked);


            /* Inventory Discard */
            /* Discard Items in Inventory */
            discardInvEnabled = config("2 - Inventory Discard", "Enabled", Toggle.On, new ConfigDescription("If on, you'll be able to discard things inside of the player inventory.", null, new ConfigurationManagerAttributes { Order = 2 }));
            lockToAdmin = config("2 - Inventory Discard", "Lock to Admin", Toggle.On, new ConfigDescription("If on, only admin's can use this feature.", null, new ConfigurationManagerAttributes { Order = 1 }));
            hotKey = config("2 - Inventory Discard", "DiscardHotkey(s)", new KeyboardShortcut(KeyCode.Delete), new ConfigDescription("The hotkey to discard an item or regain resources. Must be enabled", new AcceptableShortcuts()), false);
            returnUnknownResources = config("2 - Inventory Discard", "ReturnUnknownResources", Toggle.Off, "If on, discarding an item in the inventory will return resources if recipe is unknown");
            returnEnchantedResources = config("2 - Inventory Discard", "ReturnEnchantedResources", Toggle.Off, "If on and Epic Loot is installed, discarding an item in the inventory will return resources for Epic Loot enchantments");
            returnResources = config("2 - Inventory Discard", "ReturnResources", 1f, "Fraction of resources to return (0.0 - 1.0)");


            /* Simple Recycling */
            RecyclingRate = config("3 - Reclaiming", "RecyclingRate", 0.5f,
                "Rate at which the resources are recycled. Value must be between 0 and 1.\n" +
                "The mod always rolls *down*, so if you were supposed to get 2.5 items, you would only receive 2. If the recycling rate is 0.5 (50%), " +
                "the player will receive half of the resources they would usually need to craft the item, assuming a single item in a stack and the item " +
                "is of quality level 1. If the item is of higher quality, the resulting yield would be higher as well.");
            RecyclingRate.SettingChanged += (sender, args) =>
            {
                if (RecyclingRate.Value > 1.0f) RecyclingRate.Value = 1.0f;
                if (RecyclingRate.Value < 0f) RecyclingRate.Value = 0f;
            };
            UnstackableItemsAlwaysReturnAtLeastOneResource = config("3 - Reclaiming",
                "UnstackableItemsAlwaysReturnAtLeastOneResource", Toggle.On,
                "If enabled and recycling a specific _unstackable_ item would yield 0 of a material,\n" +
                "instead you will receive 1. If disabled, you get nothing.");

            RequireExactCraftingStationForRecycling = config("3 - Reclaiming",
                "RequireExactCraftingStationForRecycling", Toggle.On,
                "If enabled, recycling will also check for the required crafting station type and level.\n" +
                "If disabled, will ignore all crafting station requirements altogether.\n" +
                "Enabled by default, to keep things close to how Valheim operates.");

            PreventZeroResourceYields = config("3 - Reclaiming", "PreventZeroResourceYields", Toggle.On,
                "If enabled and recycling an item that would yield 0 of any material,\n" +
                "instead you will receive 1. If disabled, you get nothing.");

            AllowRecyclingUnknownRecipes = config("3 - Reclaiming", "AllowRecyclingUnknownRecipes", Toggle.Off,
                "If enabled, it will allow you to recycle items that you do not know the recipe for yet.\n" +
                "Disabled by default as this can be cheaty, but sometimes required due to people losing progress.");

            ContainerRecyclingButtonPositionJsonString = config("4 - UI",
                "ContainerButtonPosition", new Vector3(496.0f, -374.0f, -1.0f),
                "The last saved recycling button position stored in JSON");

            // UI
            ContainerRecyclingEnabled = config("4 - UI", "ContainerRecyclingEnabled",
                Toggle.On, "If enabled, the mod will display the container recycling button");

            NotifyOnSalvagingImpediments = config("4 - UI", "NotifyOnSalvagingImpediments", Toggle.On,
                "If enabled and recycling a specific item runs into any issues, the mod will print a message\n" +
                "in the center of the screen (native Valheim notification). At the time of implementation,\n" +
                "this happens in the following cases:\n" +
                " - not enough free slots in the inventory to place the resulting resources\n" +
                " - player does not know the recipe for the item\n" +
                " - if enabled, cases when `PreventZeroResourceYields` kicks in and prevent the crafting");

            EnableExperimentalCraftingTabUI = config("4 - UI", "EnableExperimentalCraftingTabUI", Toggle.On,
                "If enabled, will display the experimental work in progress crafting tab UI\n" +
                "Enabled by default.");

            HideEquippedItemsInRecyclingTab = config("4 - UI", "HideRecipesForEquippedItems", Toggle.On,
                "If enabled, it will hide equipped items in the crafting tab.\n" +
                "This does not make the item recyclable and only influences whether or not it's shown.\n" +
                "Enabled by default.");

            IgnoreItemsOnHotbar = config("4 - UI", "IgnoreItemsOnHotbar", Toggle.On,
                "If enabled, it will hide hotbar items in the crafting tab.\n" +
                "Enabled by default.");

            StationFilterEnabled = config("4 - UI", "StationFilterEnabled", Toggle.On,
                "If enabled, will filter all recycling recipes based on the crafting station\n" +
                "used to produce said item. Main purpose of this is to prevent showing food\n" +
                "as a recyclable item, but can be extended further if needed.\n" +
                "Enabled by default");

            StationFilterListString = config("4 - UI", "StationFilterList", "piece_cauldron",
                "Comma separated list of crafting stations (by their \"prefab name\")\n" +
                "recipes from which should be ignored in regards to recycling.\n" +
                "Main purpose of this is to prevent showing food as a recyclable item,\n" +
                "but can be extended further if needed.\n" +
                "\n" +
                "Full list of stations used in recipes as of 0.147.3:\n" +
                "- identifier: `forge` in game name: Forge\n" +
                "- identifier: `blackforge` in game name: Black Forge\n" +
                "- identifier: `piece_workbench` in game name: Workbench\n" +
                "- identifier: `piece_cauldron` in game name: Cauldron\n" +
                "- identifier: `piece_stonecutter` in game name: Stonecutter\n" +
                "- identifier: `piece_artisanstation` in game name: Artisan table\n" +
                "- identifier: `piece_magetable` in game name: Galdr table\n" +
                "\n" +
                "Use the identifiers, not the in game names (duh!)");

            // debug
            DebugAlwaysDumpAnalysisContext = config("zDebug", "DebugAlwaysDumpAnalysisContext", Toggle.Off,
                "If enabled will dump a complete detailed recycling report every time. This is taxing in terms\n" +
                "of performance and should only be used when debugging issues. ");
            DebugAllowSpammyLogs = config("zDebug", "DebugAllowSpammyLogs", Toggle.Off,
                "If enabled, will spam recycling checks to the console.\n" +
                "VERY. VERY. SPAMMY. Influences performance. ");


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void Start()
        {
            _containerRecyclingButton = gameObject.AddComponent<ContainerRecyclingButtonHolder>();
            _containerRecyclingButton.OnRecycleAllTriggered += ContainerRecyclingTriggered;
            RecyclingTabButtonHolder = gameObject.AddComponent<StationRecyclingTabHolder>();

            if (!Chainloader.PluginInfos.ContainsKey("randyknapp.mods.epicloot")) return;
            epicLootAssembly = Chainloader.PluginInfos["randyknapp.mods.epicloot"].Instance.GetType().Assembly;
            Recycle_N_ReclaimLogger.LogDebug("Epic Loot found, providing compatibility");
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Recycle_N_ReclaimLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                Recycle_N_ReclaimLogger.LogError($"There was an issue loading your {ConfigFileName}");
                Recycle_N_ReclaimLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        public static string Localize(string text)
        {
            return Localization.instance.Localize(text);
        }

        private void ContainerRecyclingTriggered()
        {
            var player = Player.m_localPlayer;
            var container = (Container)AccessTools.Field(typeof(InventoryGui), "m_currentContainer")
                .GetValue(InventoryGui.instance);
            if (container == null) return;
            Recycle_N_ReclaimLogger.LogDebug($"Player {player.GetPlayerName()} triggered recycling");
            Recycler.RecycleInventoryForAllRecipes(container.GetInventory(), player);
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        /* Inventory Discard */
        public static ConfigEntry<KeyboardShortcut> hotKey;
        public static ConfigEntry<Toggle> discardInvEnabled;
        public static ConfigEntry<Toggle> lockToAdmin;
        public static ConfigEntry<Toggle> returnUnknownResources;
        public static ConfigEntry<Toggle> returnEnchantedResources;
        public static ConfigEntry<float> returnResources;

        /* Simple Recycling */

        private ConfigEntry<string> _stationFilterListString;

        public static List<string> StationFilterList = new();

        private ConfigEntry<string> StationFilterListString
        {
            get => _stationFilterListString;
            set
            {
                void SplitNewValueAndSetProperty()
                {
                    StationFilterList = value.Value.Split(',')
                        .Select(entry => entry.Trim())
                        .ToList();
                }

                _stationFilterListString = value;
                value.SettingChanged += (sender, args) => { SplitNewValueAndSetProperty(); };
                SplitNewValueAndSetProperty();
            }
        }

        public static ConfigEntry<Toggle> StationFilterEnabled = null!;

        public static ConfigEntry<Toggle> EnableExperimentalCraftingTabUI = null!;

        public static ConfigEntry<Toggle> NotifyOnSalvagingImpediments = null!;

        public static ConfigEntry<Toggle> PreventZeroResourceYields = null!;

        public static ConfigEntry<Toggle> UnstackableItemsAlwaysReturnAtLeastOneResource = null!;

        public static ConfigEntry<float> RecyclingRate = null!;

        public static ConfigEntry<Toggle> ContainerRecyclingEnabled = null!;
        public static ConfigEntry<Toggle> IgnoreItemsOnHotbar = null!;
        public static ConfigEntry<Vector3> ContainerRecyclingButtonPositionJsonString = null!;
        public static ConfigEntry<Toggle> AllowRecyclingUnknownRecipes = null!;
        public static ConfigEntry<Toggle> DebugAlwaysDumpAnalysisContext = null!;
        public static ConfigEntry<Toggle> DebugAllowSpammyLogs = null!;
        public static ConfigEntry<Toggle> HideEquippedItemsInRecyclingTab = null!;
        public static ConfigEntry<Toggle> RequireExactCraftingStationForRecycling = null!;


        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSyncVar.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase> CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }
}