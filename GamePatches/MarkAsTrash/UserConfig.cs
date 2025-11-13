using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BepInEx;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

public class UserConfig
{
    private static readonly Dictionary<long, UserConfig> PlayerConfigs = new Dictionary<long, UserConfig>();

    public static UserConfig GetPlayerConfig(long playerID)
    {
        if (PlayerConfigs.TryGetValue(playerID, out UserConfig userConfig))
        {
            return userConfig;
        }
        else
        {
            userConfig = new UserConfig(playerID);
            PlayerConfigs[playerID] = userConfig;

            return userConfig;
        }
    }

    /// <summary>
    /// Create a user config for this local save file
    /// </summary>
    public UserConfig(long uid)
    {
        _configPath = Path.Combine(Paths.ConfigPath, $"{Recycle_N_ReclaimPlugin.ModName}_player_{uid}.dat");
        Load();
    }

    internal static void ResetAllTrashing()
    {
#if DEBUG
            Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug("Resetting all MarkAsTrash data!");
#endif

        _trashedSlots = new HashSet<Vector2i>();

        Save();
    }

    private static void Save()
    {
        try
        {
            using Stream stream = File.Open(_configPath, FileMode.Create);
            List<Tuple<int, int>>? tupledSlots = _trashedSlots.Select(item => new Tuple<int, int>(item.x, item.y)).ToList();

            Bf.Serialize(stream, tupledSlots);
        }
        catch (Exception e)
        {
            //Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogError($"Failed to save MarkAsTrash data: {e}");
        }
    }

    private static object TryDeserialize(Stream stream)
    {
        object result;

        try
        {
            result = Bf.Deserialize(stream);
        }
        catch (SerializationException)
        {
            result = null!;
        }

        return result;
    }

    private static void LoadProperty<T>(Stream file, out T property) where T : new()
    {
        object obj = TryDeserialize(file);

        if (obj is T property1)
        {
            property = property1;
            return;
        }

        property = Activator.CreateInstance<T>();
    }

    private void Load()
    {
        using Stream stream = File.Open(_configPath, FileMode.OpenOrCreate);
        stream.Seek(0L, SeekOrigin.Begin);

        _trashedSlots = new HashSet<Vector2i>();

        List<Tuple<int, int>>? deserializedTrashedSlots = new List<Tuple<int, int>>();
        LoadProperty(stream, out deserializedTrashedSlots);

        if (deserializedTrashedSlots != null)
            foreach (Tuple<int, int>? item in deserializedTrashedSlots)
            {
                _trashedSlots.Add(new Vector2i(item.Item1, item.Item2));
            }
    }

    public void ToggleSlotTrashing(Vector2i position)
    {
        _trashedSlots.XAdd(position);
        Save();
    }

    public void AddSlotTrashing(Vector2i position)
    {
        _trashedSlots.XAdd(position, false);
        Save();
    }

    public bool IsSlotTrashed(Vector2i position)
    {
        return _trashedSlots.Contains(position);
    }

    private static string _configPath;
    private static HashSet<Vector2i> _trashedSlots = null!;
    private static readonly BinaryFormatter Bf = new BinaryFormatter();
}

public static class CollectionExtension
{
    public static bool XAdd<T>(this HashSet<T> instance, T item, bool isToggle = true)
    {
        if (isToggle)
        {
            if (instance.Contains(item))
            {
                instance.Remove(item);
                return false;
            }

            instance.Add(item);
            return true;
        }

        if (!instance.Contains(item))
        {
            instance.Add(item);
            return true;
        }

        return false;
    }
}