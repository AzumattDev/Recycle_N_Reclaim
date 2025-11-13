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
        if (crafting || !DisplayTooltipHint.Value)
        {
            return;
        }

        StringBuilder stringBuilder = new StringBuilder(256);
        stringBuilder.Append(__result);

        UserConfig conf = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());

        if (conf.IsSlotTrashed(item.m_gridPos))
        {
            string? color = ColorUtility.ToHtmlStringRGB(BorderColorTrashedSlot.Value);

            stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{TrashedSlotTooltip.Value}</color>");
        }

        __result = stringBuilder.ToString();
    }
}