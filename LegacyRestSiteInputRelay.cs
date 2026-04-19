using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using System.Threading.Tasks;

namespace ItemLegacy;

public sealed partial class LegacyRestSiteInputRelay : Node
{
    public override void _EnterTree()
    {
        SetProcessInput(true);
        SetProcessUnhandledInput(false);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Left || mouseButton.Pressed)
        {
            return;
        }

        NRestSiteRoom? room = NRestSiteRoom.Instance;
        if (room == null || !room.IsInsideTree())
        {
            return;
        }

        NRestSiteButton? legacyButton = FindLegacyButton(room);
        bool hasModal = NModalContainer.Instance?.OpenModal != null;
        int overlayCount = NOverlayStack.Instance?.ScreenCount ?? 0;
        bool hitLegacyButton = legacyButton != null && legacyButton.GetGlobalRect().HasPoint(mouseButton.GlobalPosition);

        if (!hitLegacyButton || hasModal || overlayCount > 0)
        {
            return;
        }

        TaskHelper.RunSafely(InvokeLegacySelectionAsync(room, legacyButton!));
        GetViewport()?.SetInputAsHandled();
    }

    private static async Task InvokeLegacySelectionAsync(NRestSiteRoom room, NRestSiteButton button)
    {
        var option = button.Option;
        int index = -1;
        for (int i = 0; i < room.Options.Count; i++)
        {
            if (room.Options[i] == option)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        room.DisableOptions();
        bool success = false;
        try
        {
            success = await RunManager.Instance.RestSiteSynchronizer.ChooseLocalOption(index);
            if (success)
            {
                room.AfterSelectingOption(option);
            }
        }
        finally
        {
            if (!success && room.IsInsideTree())
            {
                await room.ToSignal(room.GetTree(), SceneTree.SignalName.ProcessFrame);
                room.EnableOptions();
            }
        }
    }

    private static NRestSiteButton? FindLegacyButton(NRestSiteRoom room)
    {
        foreach (var option in room.Options)
        {
            if (option is LegacyRestSiteOption)
            {
                return room.GetButtonForOption(option);
            }
        }

        return null;
    }
}
