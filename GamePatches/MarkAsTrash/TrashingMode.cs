namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

internal class TrashingMode
{
    private static bool hasCurrentlyToggledTrashing = false;

    internal static bool HasCurrentlyToggledTrashing
    {
        get => hasCurrentlyToggledTrashing;
        set { hasCurrentlyToggledTrashing = value; }
    }

    internal static void RefreshDisplay()
    {
        HasCurrentlyToggledTrashing |= false;
    }

    internal static bool IsInTrashingMode()
    {
        return HasCurrentlyToggledTrashing || Recycle_N_ReclaimPlugin.TrashingModifierKeybind1.Value.IsKeyHeld();
    }
}