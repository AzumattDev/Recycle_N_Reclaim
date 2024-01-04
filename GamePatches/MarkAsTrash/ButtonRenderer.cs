using UnityEngine.UI;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash
{
    internal class ButtonRenderer
    {
        internal static bool hasOpenedInventoryOnce = false;
        
        internal static Button TrashingTogglingButton = null!;

        internal class MainButtonUpdate
        {
            internal static void UpdateInventoryGuiButtons(InventoryGui __instance)
            {
                if (!hasOpenedInventoryOnce)
                {
                    return;
                }

                if (__instance != InventoryGui.instance)
                {
                    return;
                }

                if (Player.m_localPlayer)
                {
                    // reset in case player forgot to turn it off
                    TrashingMode.HasCurrentlyToggledTrashing = false;
                }
            }
        }
    }
}