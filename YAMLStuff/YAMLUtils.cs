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
        yamlData = deserializer.Deserialize<Root>(yamlInput);
        // log the yaml data
        Recycle_N_ReclaimLogger.LogDebug($"yamlData:\n{yamlInput}");
        // Iterate over each group in predefinedGroups
        foreach (KeyValuePair<string, HashSet<string>> group in predefinedGroups)
        {
            // Add each predefined group to the yamlData
            yamlData.Groups[group.Key] = group.Value.ToList();
        }
    }

    internal static void ParseGroups()
    {
        // Check if the Groups dictionary in Root has been initialized
        if (yamlData == null)
            yamlData.Groups = new Dictionary<string, List<string>>();

        if (yamlData.Groups.Any())
        {
            foreach (var group in yamlData.Groups)
            {
                string groupName = group.Key;
                if (group.Value is List<string> prefabs)
                {
                    List<string> prefabNames = new List<string>();
                    foreach (var prefab in prefabs)
                    {
                        prefabNames.Add(prefab);
                    }

                    yamlData.Groups[groupName] = prefabNames;
                }
            }
        }
    }


    public static void WriteYaml(string filePath)
    {
        var serializer = new SerializerBuilder().Build();
        using var output = new StreamWriter(filePath);
        serializer.Serialize(output, yamlData);

        // Serialize the data again to YAML format
        string serializedData = serializer.Serialize(yamlData);

        // Append the serialized YAML data to the file
        File.AppendAllText(filePath, serializedData);
    }
}