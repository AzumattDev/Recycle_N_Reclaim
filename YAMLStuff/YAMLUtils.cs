using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Recycle_N_Reclaim.YAMLStuff;

public class YAMLUtils
{
    internal static void WriteConfigFileFromResource(string configFilePath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = "Recycle_N_Reclaim.YAMLStuff.Example.yml";

        using Stream resourceStream = assembly.GetManifestResourceStream(resourceName)!;
        if (resourceStream == null)
        {
            throw new FileNotFoundException($"Resource '{resourceName}' not found in the assembly.");
        }

        using StreamReader reader = new StreamReader(resourceStream);
        string contents = reader.ReadToEnd();

        File.WriteAllText(configFilePath, contents);
    }

    internal static void ReadYaml(string yamlInput)
    {
        var deserializer = new DeserializerBuilder().Build();
        Recycle_N_ReclaimPlugin.yamlData = deserializer.Deserialize<Root>(yamlInput);
        // log the yaml data
        Recycle_N_ReclaimPlugin.Recycle_N_ReclaimLogger.LogDebug($"yamlData:\n{yamlInput}");
    }

    internal static void ParseGroups()
    {
        // Check if the Groups dictionary in Root has been initialized
        if (Recycle_N_ReclaimPlugin.yamlData == null)
            Recycle_N_ReclaimPlugin.yamlData.Groups = new Dictionary<string, List<string>>();

        if (Recycle_N_ReclaimPlugin.yamlData.Groups.Any())
        {
            foreach (var group in Recycle_N_ReclaimPlugin.yamlData.Groups)
            {
                string groupName = group.Key;
                if (group.Value is List<string> prefabs)
                {
                    List<string> prefabNames = new List<string>();
                    foreach (var prefab in prefabs)
                    {
                        prefabNames.Add(prefab);
                    }

                    Recycle_N_ReclaimPlugin.yamlData.Groups[groupName] = prefabNames;
                }
            }
        }
    }


    public static void WriteYaml(string filePath)
    {
        var serializer = new SerializerBuilder().Build();
        using var output = new StreamWriter(filePath);
        serializer.Serialize(output, Recycle_N_ReclaimPlugin.yamlData);

        // Serialize the data again to YAML format
        string serializedData = serializer.Serialize(Recycle_N_ReclaimPlugin.yamlData);

        // Append the serialized YAML data to the file
        File.AppendAllText(filePath, serializedData);
    }
}