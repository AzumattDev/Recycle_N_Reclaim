namespace Recycle_N_Reclaim.YAMLStuff;

public class Root
{
    [YamlMember(Alias = "groups")]
    public Dictionary<string, List<string>> Groups { get; set; }

    [YamlMember(Alias = "containers")]
    public Dictionary<string, excludeContainer> Containers { get; set; }

    [YamlMember(Alias = "reclaiming")]
    public Reclaiming Reclaiming { get; set; }  // Removed static

    [YamlMember(Alias = "inventory")]
    public Inventory Inventory { get; set; }  // Removed static
}

public class excludeContainer
{
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; }

    [YamlMember(Alias = "includeOverride")]
    public List<string> IncludeOverride { get; set; }
}

public class Reclaiming
{
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; }

    [YamlMember(Alias = "includeOverride")]
    public List<string> IncludeOverride { get; set; }
}

public class Inventory
{
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; }

    [YamlMember(Alias = "includeOverride")]
    public List<string> IncludeOverride { get; set; }
}