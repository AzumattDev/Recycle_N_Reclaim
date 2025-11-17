using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using JetBrains.Annotations;
using Recycle_N_Reclaim.GamePatches.UI;
using ServerSync;
using LocalizationManager;
using Recycle_N_Reclaim.GamePatches.MarkAsTrash;
#if DEBUG
using System.Text.RegularExpressions;
using System.Text;
#endif

namespace Recycle_N_Reclaim;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
public class Recycle_N_ReclaimPlugin : BaseUnityPlugin
{
    internal const string ModName = "Recycle_N_Reclaim";
    internal const string ModVersion = "1.3.10";
    internal const string Author = "Azumatt";
    private const string ModGUID = Author + "." + ModName;
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
    internal static Assembly? epicLootAssembly;
    internal static string ConnectionError = "";
    public static bool HasAuga;
    public static StationRecyclingTabHolder RecyclingTabButtonHolder { get; private set; }
    private ContainerRecyclingButtonHolder _containerRecyclingButton;

    private readonly Harmony _harmony = new(ModGUID);

    public static readonly ManualLogSource Recycle_N_ReclaimLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    internal static readonly ConfigSync ConfigSyncVar = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    internal static readonly string yamlFileName = $"{Author}.{ModName}_ExcludeLists.yml";
    internal static readonly string yamlPath = Paths.ConfigPath + Path.DirectorySeparatorChar + yamlFileName;
    internal static readonly CustomSyncedValue<string> RNRExcludeListData = new(ConfigSyncVar, "RNR_YamlData", "");

    //
    internal static Root yamlData = new Root();
    internal static Dictionary<string, HashSet<string>> predefinedGroups = new();

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    public void Awake()
    {
        // Uncomment the line below to use the LocalizationManager for localizing your mod.
        // Make sure to populate the English.yml file in the translation folder with your keys to be localized and the values associated before uncommenting!.
        Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSyncVar.AddLockingConfigEntry(_serverConfigLocked);

        ApplyCraftedBy = config("1 - General", "Apply Crafted By", Toggle.On, "If on, the player will be the 'crafter' of each recycled item. If off, these values are empty");
        
        /* Inventory Discard */
        var sectionName = "2 - Inventory Recycle";
        /* Discard Items in Inventory */
        discardInvEnabled = config(sectionName, "Enabled", Toggle.On, new ConfigDescription("If on, you'll be able to discard things inside of the player inventory.", null, new ConfigurationManagerAttributes { Order = 2 }));
        lockToAdmin = config(sectionName, "Lock to Admin", Toggle.On, new ConfigDescription("If on, only admin's can use this feature.", null, new ConfigurationManagerAttributes { Order = 1 }));
        hotKey = config(sectionName, "DiscardHotkey(s)", new KeyboardShortcut(KeyCode.Delete), new ConfigDescription("The hotkey to discard an item or regain resources. Must be enabled", new AcceptableShortcuts()), false);
        returnUnknownResources = config(sectionName, "ReturnUnknownResources", Toggle.Off, "If on, discarding an item in the inventory will return resources if recipe is unknown");
        returnEnchantedResources = config(sectionName, "ReturnEnchantedResources", Toggle.On, "If on and Epic Loot or Jewelcrafting is installed, discarding an item in the inventory will return resources for Epic Loot enchantments or Jewelcrafting gems");
        returnResources = config(sectionName, "ReturnResources", 1f, "Fraction of resources to return (0.0 - 1.0). This setting is forced to be between 0 and 1. Any higher or lower values will be set to 0 or 1 respectively.");
        returnResources.SettingChanged += (sender, args) =>
        {
            if (returnResources.Value > 1.0f) returnResources.Value = 1.0f;
            if (returnResources.Value < 0f) returnResources.Value = 0f;
        };

        // Inventory MarkAsTrash
        string TrashingKey = $"While this & right click are held, hovering over items marks them for trashing. Releasing this will cancel trashing for all items.";

        BorderColorTrashedSlot = config(sectionName, nameof(BorderColorTrashedItem), new Color(1f, 0.0f, 0f), "Color of the border for slots containing Trashed items.", false);


        DisplayTooltipHint = config(sectionName, nameof(DisplayTooltipHint), true, "Whether to add additional info the item tooltip of a Trashed or trash flagged item.", false);

        TrashingKeybind = config(sectionName, nameof(TrashingKeybind), new KeyboardShortcut(KeyCode.Mouse2), $"Key(s) that when pressed while holding your modifier key will trash all items marked as trash. Default setting is middle mouse click", false);
        TrashingModifierKeybind1 = config(sectionName, nameof(TrashingModifierKeybind1), new KeyboardShortcut(KeyCode.X), $"{TrashingKey}.", false);
        TrashedSlotTooltip = config(sectionName, nameof(TrashedSlotTooltip), "Slot is Trashed and will be a part of the bulk delete", string.Empty, false);

        /* Reclaiming */
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

        returnEnchantedResourcesReclaiming = config("3 - Reclaiming", "ReturnEnchantedResources", Toggle.On, "If on and Epic Loot or Jewelcrafting is installed, discarding an item in the inventory will return resources for Epic Loot enchantments or Jewelcrafting gems");

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
            "Full list of stations used in recipes as of 0.216.9:\n" +
            "- identifier: `forge` in game name: Forge\n" +
            "- identifier: `blackforge` in game name: Black Forge\n" +
            "- identifier: `piece_workbench` in game name: Workbench\n" +
            "- identifier: `piece_cauldron` in game name: Cauldron\n" +
            "- identifier: `piece_stonecutter` in game name: Stonecutter\n" +
            "- identifier: `piece_artisanstation` in game name: Artisan table\n" +
            "- identifier: `piece_magetable` in game name: Galdr table\n");

        // debug
        DebugAlwaysDumpAnalysisContext = config("zDebug", "DebugAlwaysDumpAnalysisContext", Toggle.Off,
            "If enabled will dump a complete detailed recycling report every time. This is taxing in terms\n" +
            "of performance and should only be used when debugging issues. ");
        DebugAllowSpammyLogs = config("zDebug", "DebugAllowSpammyLogs", Toggle.Off,
            "If enabled, will spam recycling checks to the console.\n" +
            "VERY. VERY. SPAMMY. Influences performance. ");


        if (!File.Exists(yamlPath))
        {
            YAMLUtils.WriteConfigFileFromResource(yamlPath);
        }

        RNRExcludeListData.ValueChanged += OnValChangedUpdate; // check for file changes
        RNRExcludeListData.AssignLocalValue(File.ReadAllText(yamlPath));


        Assembly assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupWatcher();
    }

    private void Start()
    {
        InventoryGridUpdateGuiPatch.border = loadSprite("trashingborder.png");
        AutoDoc();
        HasAuga = Auga.API.IsLoaded();
        _containerRecyclingButton = gameObject.AddComponent<ContainerRecyclingButtonHolder>();
        _containerRecyclingButton.OnRecycleAllTriggered += ContainerRecyclingTriggered;
        RecyclingTabButtonHolder = gameObject.AddComponent<StationRecyclingTabHolder>();

        if (!Chainloader.PluginInfos.ContainsKey("randyknapp.mods.epicloot")) return;
        epicLootAssembly = Chainloader.PluginInfos["randyknapp.mods.epicloot"].Instance.GetType().Assembly;
        Recycle_N_ReclaimLogger.LogDebug("Epic Loot found, providing compatibility");
    }

    internal static Sprite loadSprite(string name)
    {
        Texture2D texture = loadTexture(name);
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        return null!;
    }

    private static Texture2D loadTexture(string name)
    {
        Texture2D texture = new(0, 0);

        texture.LoadImage(ReadEmbeddedFileBytes("Assets." + name));
        return texture!;
    }

    private static byte[] ReadEmbeddedFileBytes(string name)
    {
        using MemoryStream stream = new();
        Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + "." + name)!.CopyTo(stream);
        return stream.ToArray();
    }

    private void AutoDoc()
    {
#if DEBUG
            // Store Regex to get all characters after a [
            Regex regex = new(@"\[(.*?)\]");

            // Strip using the regex above from Config[x].Description.Description
            string Strip(string x) => regex.Match(x).Groups[1].Value;
            StringBuilder sb = new();
            string lastSection = "";
            foreach (ConfigDefinition x in Config.Keys)
            {
                // skip first line
                if (x.Section != lastSection)
                {
                    lastSection = x.Section;
                    sb.Append($"{Environment.NewLine}`{x.Section}`{Environment.NewLine}");
                }

                sb.Append($"\n{x.Key} [{Strip(Config[x].Description.Description)}]" +
                          $"{Environment.NewLine}   * {Config[x].Description.Description.Replace("[Synced with Server]", "").Replace("[Not Synced with Server]", "")}" +
                          $"{Environment.NewLine}     * Default Value: {Config[x].GetSerializedValue()}{Environment.NewLine}");
            }

            File.WriteAllText(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{ModName}_AutoDoc.md"),
                sb.ToString());
#endif
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

        FileSystemWatcher yamlwatcher = new(Paths.ConfigPath, yamlFileName);
        yamlwatcher.Changed += ReadYamlFiles;
        yamlwatcher.Created += ReadYamlFiles;
        yamlwatcher.Renamed += ReadYamlFiles;
        yamlwatcher.IncludeSubdirectories = true;
        yamlwatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        yamlwatcher.EnableRaisingEvents = true;
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

    private void ReadYamlFiles(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(yamlPath)) return;
        try
        {
            Recycle_N_ReclaimLogger.LogDebug("ReadConfigValues called");
            RNRExcludeListData.AssignLocalValue(File.ReadAllText(yamlPath));
        }
        catch
        {
            Recycle_N_ReclaimLogger.LogError($"There was an issue loading your {yamlFileName}");
            Recycle_N_ReclaimLogger.LogError("Please check your entries for spelling and format!");
        }
    }

    private static void OnValChangedUpdate()
    {
        Recycle_N_ReclaimLogger.LogDebug("OnValChanged called");
        try
        {
            YAMLUtils.ReadYaml(RNRExcludeListData.Value);
            //YAMLUtils.ParseGroups();
        }
        catch (Exception e)
        {
            Recycle_N_ReclaimLogger.LogError($"Failed to deserialize {yamlFileName}: {e}");
        }
    }

    public static string Localize(string text)
    {
        return Localization.instance.Localize(text);
    }

    public static string Localize(string text, params string[] words)
    {
        return Localization.instance.Localize(text, words);
    }

    private void ContainerRecyclingTriggered()
    {
        var player = Player.m_localPlayer;
        var container = (Container)AccessTools.Field(typeof(InventoryGui), "m_currentContainer")
            .GetValue(InventoryGui.instance);
        if (container == null) return;
        Recycle_N_ReclaimLogger.LogDebug($"Player {player.GetPlayerName()} triggered recycling");
        Reclaimer.RecycleInventoryForAllRecipes(container.GetInventory(), GroupUtils.GetPrefabName(container.transform.name), player);
    }


    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    public static ConfigEntry<Toggle> ApplyCraftedBy = null!;

    /* Inventory Discard */
    public static ConfigEntry<KeyboardShortcut> hotKey = null!;
    public static ConfigEntry<Toggle> discardInvEnabled = null!;
    public static ConfigEntry<Toggle> lockToAdmin = null!;
    public static ConfigEntry<Toggle> returnUnknownResources = null!;
    public static ConfigEntry<Toggle> returnEnchantedResources = null!;
    public static ConfigEntry<float> returnResources = null!;

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
    public static ConfigEntry<Toggle> returnEnchantedResourcesReclaiming = null!;

    // Inventory MarkAsTrash


    public static ConfigEntry<Color> BorderColorTrashedItem = null!;
    public static ConfigEntry<Color> BorderColorTrashedSlot = null!;
    public static ConfigEntry<bool> DisplayTooltipHint = null!;
    public static ConfigEntry<KeyboardShortcut> TrashingModifierKeybind1 = null!;
    public static ConfigEntry<KeyboardShortcut> TrashingKeybind = null!;
    public static ConfigEntry<string> TrashedSlotTooltip = null!;


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

public static class KeyboardExtensions
{
    public static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}

public static class ToggleExtensions
{
    public static bool IsOn(this Recycle_N_ReclaimPlugin.Toggle toggle)
    {
        return toggle == Recycle_N_ReclaimPlugin.Toggle.On;
    }

    public static bool IsOff(this Recycle_N_ReclaimPlugin.Toggle toggle)
    {
        return toggle == Recycle_N_ReclaimPlugin.Toggle.Off;
    }
}