using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Network.Structures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dagobert
{
  internal unsafe sealed class MarketBoardHandler : IDisposable
  {
    private readonly Lumina.Excel.ExcelSheet<Item> _items;
    private bool _newRequest;
    private bool _useHq;
    private bool _itemHq;
    private int _lastRequestId = -1;

    // Listings arrive in batches of ~10 per OfferingsReceived event for the same
    // RequestId. BaitGuard needs the full set to compute a meaningful price floor,
    // so we accumulate per request and re-evaluate as each batch lands.
    private int _bufferedRequestId = -1;
    private readonly List<IMarketBoardItemListing> _bufferedListings = new();

    private int NewPrice
    {
      get => _newPrice;
      set
      {
        _newPrice = value;
        NewPriceReceived?.Invoke(this, new NewPriceEventArgs(NewPrice));
      }
    }
    private int _newPrice;

    public event EventHandler<NewPriceEventArgs>? NewPriceReceived;

    public MarketBoardHandler()
    {
      _items = Svc.Data.GetExcelSheet<Item>();

      Plugin.MarketBoard.OfferingsReceived += MarketBoardOnOfferingsReceived;

      Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", AddonRetainerSellPostSetup);
      Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ItemSearchResult", ItemSearchResultPostSetup);
    }

    public void Dispose()
    {
      Plugin.MarketBoard.OfferingsReceived -= MarketBoardOnOfferingsReceived;
      Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", AddonRetainerSellPostSetup);
      Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ItemSearchResult", ItemSearchResultPostSetup);
    }

    private void MarketBoardOnOfferingsReceived(IMarketBoardCurrentOfferings currentOfferings)
    {
      if (!_newRequest)
        return;

      if (currentOfferings.RequestId == _lastRequestId)
      {
        NewPrice = -1;
        return;
      }

      AccumulateBatch(currentOfferings);

      if (_bufferedListings.Count == 0)
      {
        NewPrice = -1;
        return;
      }

      var hqEligible = BuildHqEligibleIndices();
      if (hqEligible.Count == 0)
      {
        NewPrice = -1; // wait for more incoming offerings
        return;
      }

      int? priceDecision = DecidePrice(hqEligible);
      if (priceDecision is null)
      {
        NewPrice = -1; // no credible target yet — wait for more batches
        return;
      }

      NewPrice = priceDecision.Value;
      _lastRequestId = currentOfferings.RequestId;
      _newRequest = false;
    }

    private int? DecidePrice(List<int> hqEligible)
    {
      var opts = BuildOptions();

      // UndercutSelf=true: own retainers compete on equal footing — bait guard sees
      // them, undercut applies to whatever it picks. Simpler path.
      if (Plugin.Configuration.UndercutSelf)
      {
        int? target = BaitGuard.SelectTargetIndex(_bufferedListings, hqEligible, opts);
        return target is null ? null : Undercut(_bufferedListings[target.Value].PricePerUnit);
      }

      // UndercutSelf=false: run bait guard over competitors only, then check whether
      // our own retainer is already at or below the credible competitor's price.
      // If so, hold position (price-match own, which is a no-op for the addon).
      var competitors = hqEligible
        .Where(i => !IsOwnRetainer(_bufferedListings[i].RetainerId))
        .ToList();
      var ownIndices = hqEligible
        .Where(i => IsOwnRetainer(_bufferedListings[i].RetainerId))
        .ToList();
      uint? ownLowest = ownIndices.Count == 0
        ? null
        : ownIndices.Min(i => _bufferedListings[i].PricePerUnit);

      int? competitorIdx = BaitGuard.SelectTargetIndex(_bufferedListings, competitors, opts);

      if (competitorIdx is null)
      {
        // No credible competitor visible. If we have own listings, hold at own's
        // current lowest (matches the original behavior of price-matching when
        // listings[0] was your own retainer). Otherwise, wait for more batches.
        return ownLowest is null ? null : (int)ownLowest.Value;
      }

      var competitorPrice = _bufferedListings[competitorIdx.Value].PricePerUnit;
      if (ownLowest is not null && ownLowest.Value <= competitorPrice)
        return (int)ownLowest.Value; // already competitive — no change

      return Undercut(competitorPrice);
    }

    private static int Undercut(uint competitorPricePerUnit)
    {
      int competitor = (int)competitorPricePerUnit;
      return Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount
        ? Math.Max(competitor - Plugin.Configuration.UndercutAmount, 1)
        : Math.Max((100 - Plugin.Configuration.UndercutAmount) * competitor / 100, 1);
    }

    private static BaitGuard.Options BuildOptions() => new(
      Enabled: Plugin.Configuration.EnableBaitGuard,
      FloorPercent: Plugin.Configuration.BaitGuardFloorPercent,
      SampleListings: Plugin.Configuration.BaitGuardSampleListings,
      GapPercent: Plugin.Configuration.BaitGuardGapPercent,
      MinQuantity: Plugin.Configuration.BaitGuardMinQuantity);

    private void AccumulateBatch(IMarketBoardCurrentOfferings currentOfferings)
    {
      if (_bufferedRequestId != currentOfferings.RequestId)
      {
        _bufferedListings.Clear();
        _bufferedRequestId = currentOfferings.RequestId;
      }

      foreach (var l in currentOfferings.ItemListings)
        _bufferedListings.Add(l);
    }

    private List<int> BuildHqEligibleIndices()
    {
      var indices = new List<int>(_bufferedListings.Count);
      bool requireHq = _useHq
                       && _bufferedListings.Count > 0
                       && _items.Single(j => j.RowId == _bufferedListings[0].ItemId).CanBeHq;

      for (int i = 0; i < _bufferedListings.Count; i++)
      {
        if (requireHq && !_bufferedListings[i].IsHq)
          continue;
        indices.Add(i);
      }

      return indices;
    }

    private void ItemSearchResultPostSetup(AddonEvent type, AddonArgs args)
    {
      _newRequest = true;
      _useHq = Plugin.Configuration.HQ && _itemHq;
      _bufferedListings.Clear();
      _bufferedRequestId = -1;
    }

    private unsafe void AddonRetainerSellPostSetup(AddonEvent type, AddonArgs args)
    {
      string nodeText = ((AddonRetainerSell*)args.Addon.Address)->ItemName->NodeText.ToString();
      _itemHq = nodeText.Contains('');
    }

    public void PopulateRetainerCache()
    {
      bool changed = false;
      var retainerManager = RetainerManager.Instance();

      for (uint i = 0; i < retainerManager->GetRetainerCount(); ++i)
      {
        if (!Plugin.Configuration.SeenRetainers.Contains(retainerManager->GetRetainerBySortedIndex(i)->RetainerId))
        {
          Plugin.Configuration.SeenRetainers.Add(retainerManager->GetRetainerBySortedIndex(i)->RetainerId);
          changed = true;
        }

      }

      if (changed)
        Plugin.Configuration.Save();
    }

    private static bool IsOwnRetainer(ulong retainerId) => Plugin.Configuration.SeenRetainers.Contains(retainerId);
  }
}
