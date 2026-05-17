using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;

namespace ItemLegacy;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
public static class LegacyRestSiteOptionGeneratePatch
{
    public static void Postfix(Player player, List<RestSiteOption> __result)
    {
        if (__result.Exists(static option => option is LegacyRestSiteOption))
        {
            return;
        }

        __result.Add(new LegacyRestSiteOption(player));
    }
}

[HarmonyPatch(typeof(NRestSiteButton), "Reload")]
public static class LegacyRestSiteButtonReloadPatch
{
    private static readonly AccessTools.FieldRef<NRestSiteButton, TextureRect> IconField =
        AccessTools.FieldRefAccess<NRestSiteButton, TextureRect>("_icon");

    private static readonly AccessTools.FieldRef<NRestSiteButton, MegaLabel> LabelField =
        AccessTools.FieldRefAccess<NRestSiteButton, MegaLabel>("_label");

    private static readonly AccessTools.FieldRef<NRestSiteButton, ShaderMaterial> HsvField =
        AccessTools.FieldRefAccess<NRestSiteButton, ShaderMaterial>("_hsv");

    public static bool Prefix(NRestSiteButton __instance)
    {
        if (__instance.Option is not LegacyRestSiteOption option)
        {
            return true;
        }

        if (!__instance.IsNodeReady())
        {
            return false;
        }

        __instance.MouseFilter = Control.MouseFilterEnum.Stop;
        IconField(__instance).Texture = PreloadManager.Cache.GetTexture2D(LegacyRestSiteOption.FallbackIconPath);

        MegaLabel label = LabelField(__instance);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SetTextAutoSize(LegacyRestSiteOption.TitleText);

        ShaderMaterial hsv = HsvField(__instance);
        if (option.IsEnabled)
        {
            hsv.SetShaderParameter("s", 1f);
            hsv.SetShaderParameter("v", 1f);
            AttachInputRelayFromButton(__instance);
        }
        else
        {
            hsv.SetShaderParameter("s", 0f);
            hsv.SetShaderParameter("v", 0.6f);
        }

        return false;
    }

    private static void AttachInputRelayFromButton(NRestSiteButton button)
    {
        NRestSiteRoom? room = FindAncestorRoom(button);
        if (room == null)
        {
            return;
        }

        if (room.GetNodeOrNull<LegacyRestSiteInputRelay>("LegacyRestSiteInputRelay") != null)
        {
            return;
        }

        room.AddChild(new LegacyRestSiteInputRelay
        {
            Name = "LegacyRestSiteInputRelay",
        });
    }

    private static NRestSiteRoom? FindAncestorRoom(Node node)
    {
        Node? current = node;
        while (current != null)
        {
            if (current is NRestSiteRoom room)
            {
                return room;
            }

            current = current.GetParent();
        }

        return null;
    }
}

[HarmonyPatch(typeof(RestSiteOption), "get_Icon")]
public static class LegacyRestSiteOptionIconPatch
{
    public static bool Prefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is not LegacyRestSiteOption)
        {
            return true;
        }

        __result = PreloadManager.Cache.GetTexture2D(LegacyRestSiteOption.FallbackIconPath);
        return false;
    }
}

[HarmonyPatch(typeof(NRestSiteButton), nameof(NRestSiteButton.RefreshTextState))]
public static class LegacyRestSiteButtonRefreshTextStatePatch
{
    private static readonly AccessTools.FieldRef<NRestSiteButton, bool> ExecutingOptionField =
        AccessTools.FieldRefAccess<NRestSiteButton, bool>("_executingOption");

    public static bool Prefix(NRestSiteButton __instance)
    {
        if (__instance.Option is not LegacyRestSiteOption option)
        {
            return true;
        }

        bool isFocused = Traverse.Create(__instance).Property("IsFocused").GetValue<bool>();
        bool isExecuting = ExecutingOptionField(__instance);
        NRestSiteRoom? room = NRestSiteRoom.Instance;
        if (room == null)
        {
            return false;
        }

        if (isFocused || isExecuting)
        {
            room.SetText(option.DisplayDescription);
        }
        else
        {
            room.FadeOutOptionDescription();
        }

        return false;
    }
}

// 新版游戏 NLinkedRewardSet.Reload 把 RewardClaimed (1 参) 信号用 Callable.From(无参) 接,
// 触发时 Godot 抛 ArgCountMismatch 把 callable 吞掉,导致 NLinkedRewardSet.GetReward 不被调,
// 链式奖励组的 UI 永远不会通过 _rewardsScreen.RewardCollectedFrom 被关闭,从而留下「不能跳过、点也无效」的残留窗口。
// 这里 Prefix 重写 Reload,用 1 参 callable 重新接信号修复这条链路。
[HarmonyPatch(typeof(NLinkedRewardSet), "Reload")]
public static class LegacyLinkedRewardSetReloadPatch
{
    private static readonly AccessTools.FieldRef<NLinkedRewardSet, NRewardsScreen> RewardsScreenField =
        AccessTools.FieldRefAccess<NLinkedRewardSet, NRewardsScreen>("_rewardsScreen");

    private static readonly AccessTools.FieldRef<NLinkedRewardSet, Control> RewardContainerField =
        AccessTools.FieldRefAccess<NLinkedRewardSet, Control>("_rewardContainer");

    private static readonly AccessTools.FieldRef<NLinkedRewardSet, Control> ChainsContainerField =
        AccessTools.FieldRefAccess<NLinkedRewardSet, Control>("_chainsContainer");

    private static readonly string ChainImagePath = ImageHelper.GetImagePath("/ui/reward_screen/reward_chain.png");

    public static bool Prefix(NLinkedRewardSet __instance)
    {
        if (!__instance.IsNodeReady())
        {
            return false;
        }

        Control rewardContainer = RewardContainerField(__instance);
        Control chainsContainer = ChainsContainerField(__instance);
        NRewardsScreen rewardsScreen = RewardsScreenField(__instance);

        foreach (Node child in rewardContainer.GetChildren())
        {
            rewardContainer.RemoveChild(child);
            child.QueueFreeSafely();
        }

        foreach (Node child in chainsContainer.GetChildren())
        {
            chainsContainer.RemoveChild(child);
            child.QueueFreeSafely();
        }

        for (int i = 0; i < __instance.LinkedRewardSet.Rewards.Count; i++)
        {
            Reward reward = __instance.LinkedRewardSet.Rewards[i];
            NRewardButton rewardButton = NRewardButton.Create(reward, rewardsScreen);
            rewardButton.CustomMinimumSize -= Vector2.Right * 20f;
            rewardContainer.AddChildSafely(rewardButton);
            rewardButton.Connect(NRewardButton.SignalName.RewardClaimed, Callable.From<NRewardButton>(_ => OnLinkedRewardClaimed(__instance)));

            if (i >= __instance.LinkedRewardSet.Rewards.Count - 1)
            {
                continue;
            }

            TextureRect chain = new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Texture = PreloadManager.Cache.GetCompressedTexture2D(ChainImagePath),
                Size = Vector2.One * 50f,
            };
            chainsContainer.AddChildSafely(chain);
            chain.GlobalPosition = chainsContainer.GlobalPosition + Vector2.Down * i * (3f + rewardButton.CustomMinimumSize.Y);
        }

        return false;
    }

    private static void OnLinkedRewardClaimed(NLinkedRewardSet linkedRewardSetControl)
    {
        NRewardsScreen rewardsScreen = RewardsScreenField(linkedRewardSetControl);
        rewardsScreen.RewardCollectedFrom(linkedRewardSetControl);
        linkedRewardSetControl.LinkedRewardSet.OnSkipped();
        linkedRewardSetControl.QueueFreeSafely();
    }
}
