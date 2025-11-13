namespace Recycle_N_Reclaim.GamePatches.Recycling;

public class RecyclingAnalysisContext
{
    public ItemDrop.ItemData Item;

    public Recipe Recipe { get; set; } = null!;
    public List<string> RecyclingImpediments { get; } = new();

    public List<string> DisplayImpediments { get; } = new();
    public List<ReclaimingYieldEntry> Entries { get; } = new();

    public bool ShouldErrorDumpAnalysis { get; set; }

    public RecyclingAnalysisContext(ItemDrop.ItemData item)
    {
        Item = item;
    }

    public readonly struct ReclaimingYieldEntry
    {
        public readonly GameObject Prefab;
        public readonly ItemDrop.ItemData RecipeItemData;
        public readonly int Amount;
        public readonly int mQuality;
        public readonly int mVariant;
        public readonly bool InitialRecipeHadZero;

        public ReclaimingYieldEntry(GameObject prefab, ItemDrop.ItemData itemData, int amount, int mQuality, int mVariant, bool initialRecipeHadZero)
        {
            Prefab = prefab;
            RecipeItemData = itemData;
            Amount = amount;
            this.mQuality = mQuality;
            this.mVariant = mVariant;
            InitialRecipeHadZero = initialRecipeHadZero;
        }
    }

    public void Dump()
    {
        if (!ShouldErrorDumpAnalysis) return; // Early return if analysis dumping is not required
        var dumpObject = new
        {
            ItemName = Item.m_shared.m_name,
            ItemQuality = Item.m_quality,
            ItemStacks = Item.m_stack,
            ItemMaxStacks = Item.m_shared.m_maxStackSize,
            Impediments = RecyclingImpediments,
            UsedRecipe = GetRecipeObject(),
            Entries = Entries.Select(entry => new
            {
                PrefabName = entry.Prefab.name,
                Amount = entry.Amount,
                Quality = entry.mQuality,
                Variant = entry.mVariant,
            }).ToList()
        };
        var sb = new StringBuilder();
        sb.AppendLine("\n==== Dump of recycling analysis was requested ====");
        sb.AppendLine(ObjectDumper.Dump(dumpObject));
        sb.AppendLine("==== Dump ends here ====");
        Recycle_N_ReclaimLogger.LogError(sb.ToString());
    }

    private dynamic GetRecipeObject()
    {
        return new
        {
            ItemName = Recipe.m_item.m_itemData.m_shared.m_name,
            RecipeName = Recipe.name,
            RecipeCraftedAmount = Recipe.m_amount,
            Resources = Recipe.m_resources.Select(resource => new
            {
                Name = resource.m_resItem.m_itemData.m_shared.m_name,
                Amount = resource.m_amount,
                AmountPerLevel = resource.m_amountPerLevel,
                Quality = resource.m_resItem.m_itemData.m_quality,
                Variant = resource.m_resItem.m_itemData.m_variant,
            }).ToList()
        };
    }
}