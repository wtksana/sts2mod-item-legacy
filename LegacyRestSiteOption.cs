using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace ItemLegacy;

public sealed class LegacyRestSiteOption : RestSiteOption
{
    public const string LegacyOptionId = "LEGACY";

    public const string TitleText = "遗产";

    public const string FallbackIconPath = "res://images/ui/rest_site/option_smith.png";

    private readonly LegacyHistoryService.OfferPlan? _offerPlan;

    public override string OptionId => LegacyOptionId;

    public override LocString Description => new("rest_site_ui", "PROMPT");

    public string DisplayDescription { get; }

    public LegacyRestSiteOption(Player owner)
        : base(owner)
    {
        if (LegacyHistoryService.TryCreateOfferPlan(owner, out LegacyHistoryService.OfferPlan? plan, out string disabledReason) && plan != null)
        {
            _offerPlan = plan;
            DisplayDescription = plan.DescriptionText;
            IsEnabled = true;
        }
        else
        {
            _offerPlan = null;
            DisplayDescription = disabledReason;
            IsEnabled = false;
        }
    }

    public override async Task<bool> OnSelect()
    {
        if (_offerPlan == null)
        {
            return false;
        }

        return await LegacyHistoryService.OfferAsync(Owner, _offerPlan);
    }
}
