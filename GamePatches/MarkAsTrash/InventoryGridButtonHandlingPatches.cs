using HarmonyLib;

namespace Recycle_N_Reclaim.GamePatches.MarkAsTrash;

[HarmonyPatch(typeof(InventoryGrid))]
internal class InventoryGridButtonHandlingPatches
{
    [HarmonyPatch(nameof(InventoryGrid.OnLeftClick)), HarmonyPrefix]
    internal static bool OnLeftClick(InventoryGrid __instance, UIInputHandler clickHandler)
    {
        return HandleClick(__instance, clickHandler, true);
    }

    internal static bool HandleClick(InventoryGrid __instance, UIInputHandler clickHandler, bool isLeftClick)
    {
        Vector2i buttonPos = __instance.GetButtonPos(clickHandler.gameObject);

        return HandleClick(__instance, buttonPos, isLeftClick);
    }

    [HarmonyPatch(nameof(InventoryGrid.UpdateGamepad))]
    private static void Postfix(InventoryGrid __instance)
    {
        if (__instance != InventoryGui.instance.m_playerGrid)
        {
            return;
        }

        if (!__instance.m_uiGroup.IsActive)
        {
            return;
        }

        if (ZInput.GetButtonDown("JoyButtonA"))
        {
            HandleClick(__instance, __instance.m_selected, true);
        }
        else if (ZInput.GetButtonDown("JoyButtonX"))
        {
            HandleClick(__instance, __instance.m_selected, false);
        }
    }

    internal static bool HandleClick(InventoryGrid __instance, Vector2i buttonPos, bool isLeftClick)
    {
        if (InventoryGui.instance.m_playerGrid != __instance)
        {
            return true;
        }

        Player localPlayer = Player.m_localPlayer;

        if (localPlayer.IsTeleporting())
        {
            return true;
        }

        if (InventoryGui.instance.m_dragGo)
        {
            return true;
        }

        if (!TrashingMode.IsInTrashingMode())
        {
            return true;
        }

        if (buttonPos == new Vector2i(-1, -1))
        {
            return true;
        }

        if (isLeftClick)
        {
            bool flag1 = __instance.m_uiGroup.IsActive && ZInput.IsGamepadActive();
            InventoryGrid.Element element1 = flag1 ? __instance.GetElement(__instance.m_selected.x, __instance.m_selected.y, __instance.m_inventory.GetWidth()) : __instance.GetHoveredElement();
            if (element1 is { m_used: true })
            {
                UserConfig.GetPlayerConfig(localPlayer.GetPlayerID()).ToggleSlotTrashing(buttonPos);
            }
        }

        return false;
    }
}