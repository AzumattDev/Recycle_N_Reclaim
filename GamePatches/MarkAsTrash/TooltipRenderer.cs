namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

[HarmonyPatch(typeof(ItemDrop.ItemData))]
internal static class TooltipRenderer
{
    [HarmonyPatch(nameof(ItemDrop.ItemData.GetTooltip), new Type[]
    {
        typeof(ItemDrop.ItemData),
        typeof(int),
        typeof(bool),
        typeof(float),
        typeof(int)
    })]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, bool crafting, ref string __result)
    {
        if (crafting || Player.m_localPlayer == null) return;
        if (!DisplayTooltipHint.Value && !ShowRecycleYieldInTooltip.Value) return;

        StringBuilder stringBuilder = new StringBuilder(256);
        stringBuilder.Append(__result);

        if (DisplayTooltipHint.Value)
        {
            UserConfig conf = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());
            if (conf.IsSlotTrashed(item.m_gridPos))
            {
                string? color = ColorUtility.ToHtmlStringRGB(BorderColorTrashedSlot.Value);
                stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{TrashedSlotTooltip.Value}</color>");
            }
        }

        if (ShowRecycleYieldInTooltip.Value)
        {
            var context = new RecyclingAnalysisContext(item);
            if (Reclaimer.TryAnalyzeOneItem(context, Player.m_localPlayer.GetInventory(), Player.m_localPlayer))
            {
                var yieldEntries = context.Entries.Where(e => e.Amount > 0).ToList();
                if (yieldEntries.Count > 0)
                {
                    stringBuilder.Append($"\n\n<color=orange>{Localize("$azumatt_recycle_n_reclaim_tooltip_yield_header")}</color>");
                    foreach (var entry in yieldEntries)
                        stringBuilder.Append($"\n  {entry.Amount}x {Localize(entry.RecipeItemData.m_shared.m_name)}");

                    if (context.RecyclingImpediments.Count > 0)
                        stringBuilder.Append($"\n<color=red>{string.Join(", ", context.RecyclingImpediments)}</color>");
                }
            }
        }

        __result = stringBuilder.ToString();
    }
}