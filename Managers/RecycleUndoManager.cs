namespace Recycle_N_Reclaim.Managers;

internal static class RecycleUndoManager
{
    private static ItemDrop.ItemData? _lastItem;
    private static List<RecyclingAnalysisContext.ReclaimingYieldEntry> _lastEntries = new();
    private static float _recordedAt = float.MinValue;

    internal static bool CanUndo => _lastItem != null && UndoRecycleGracePeriodSeconds.Value > 0 && Time.time - _recordedAt <= UndoRecycleGracePeriodSeconds.Value;

    internal static void Record(ItemDrop.ItemData item, List<RecyclingAnalysisContext.ReclaimingYieldEntry> entries)
    {
        _lastItem = item.Clone();
        _lastEntries = entries.Where(e => e.Amount > 0 && !e.InitialRecipeHadZero).ToList();
        _recordedAt = Time.time;
    }

    internal static void TryUndo()
    {
        var player = Player.m_localPlayer;
        if (player == null) return;

        if (!CanUndo)
        {
            player.Message(MessageHud.MessageType.Center, Localize("$azumatt_recycle_n_reclaim_undo_expired"));
            Clear();
            return;
        }

        var inventory = player.GetInventory();

        // Pre-check: verify all returned resources are still present before removing any
        foreach (var entry in _lastEntries)
        {
            if (inventory.CountItems(entry.RecipeItemData.m_shared.m_name) < entry.Amount)
            {
                player.Message(MessageHud.MessageType.Center, Localize("$azumatt_recycle_n_reclaim_undo_failed"));
                Clear();
                return;
            }
        }

        // Remove returned resources, tracking what was actually removed for rollback
        var removedEntries = new List<RecyclingAnalysisContext.ReclaimingYieldEntry>();
        foreach (var entry in _lastEntries)
        {
            inventory.RemoveItem(entry.RecipeItemData.m_shared.m_name, entry.Amount);
            removedEntries.Add(entry);
        }

        var item = _lastItem!;
        ItemDrop.ItemData? restored = item.m_dropPrefab != null
            ? inventory.AddItem(item.m_dropPrefab.name, item.m_stack, item.m_quality, item.m_variant, item.m_crafterID, item.m_crafterName)
            : null;

        if (restored == null)
        {
            // Original item could not be re-added (e.g. resources stacked, no free slot for item).
            // Restore the resources that were removed so the player doesn't lose anything.
            bool restorationIncomplete = false;
            foreach (var entry in removedEntries)
            {
                var result = inventory.AddItem(entry.Prefab.name, entry.Amount, entry.mQuality, entry.mVariant, 0, "");
                if (result == null)
                {
                    restorationIncomplete = true;
                    Recycle_N_ReclaimLogger.LogError($"Undo rollback: failed to restore {entry.Amount}x {entry.Prefab.name} to inventory.");
                }
            }

            if (restorationIncomplete)
                Recycle_N_ReclaimLogger.LogError("Undo rollback was incomplete. One or more resources could not be returned to inventory.");

            player.Message(MessageHud.MessageType.Center, Localize("$azumatt_recycle_n_reclaim_undo_failed"));
            Clear();
            return;
        }

        player.Message(MessageHud.MessageType.Center, Localize("$azumatt_recycle_n_reclaim_undo_success"));
        Clear();
    }

    internal static void Clear()
    {
        _lastItem = null;
        _lastEntries.Clear();
    }
}
