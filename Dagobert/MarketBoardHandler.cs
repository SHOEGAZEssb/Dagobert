using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Network.Structures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;

namespace Dagobert
{
  internal unsafe sealed class MarketBoardHandler : IDisposable
  {
    private const int ListingsPerBatch = 10;

    private readonly Lumina.Excel.ExcelSheet<Item> _items;
    private bool _newRequest;
    private bool _useHq;
    private bool _itemHq;
    private int _lastRequestId = -1;
    private int _pendingNoMatchRequestId = -1;
    private long _pendingNoMatchTimeoutAt;

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
      Svc.Framework.Update += FrameworkOnUpdate;

      Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RetainerSell", AddonRetainerSellPostSetup);
      Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ItemSearchResult", ItemSearchResultPostSetup);
    }

    public void Dispose()
    {
      Plugin.MarketBoard.OfferingsReceived -= MarketBoardOnOfferingsReceived;
      Svc.Framework.Update -= FrameworkOnUpdate;
      Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "RetainerSell", AddonRetainerSellPostSetup);
      Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ItemSearchResult", ItemSearchResultPostSetup);
    }

    private void MarketBoardOnOfferingsReceived(IMarketBoardCurrentOfferings currentOfferings)
    {
      if (!_newRequest)
        return;

      if (currentOfferings.RequestId == _lastRequestId)
        return;

      if (currentOfferings.ItemListings.Count == 0)
      {
        CompletePriceRequest(-1, currentOfferings.RequestId);
        return;
      }

      var skipNq = _useHq && _items.Single(j => j.RowId == currentOfferings.ItemListings[0].ItemId).CanBeHq;
      var i = 0;
      while (i < currentOfferings.ItemListings.Count)
      {
        if (skipNq && !currentOfferings.ItemListings[i].IsHq)
          i++;
        else
          break;
      }

      // offerings arrive in batches of 10; if no match in this batch, wait for the next
      if (i >= currentOfferings.ItemListings.Count)
      {
        if (currentOfferings.ItemListings.Count < ListingsPerBatch)
        {
          CompletePriceRequest(-1, currentOfferings.RequestId);
          return;
        }

        _pendingNoMatchRequestId = currentOfferings.RequestId;
        _pendingNoMatchTimeoutAt = Environment.TickCount64 + GetNoMatchTimeoutMs();
        return;
      }

      ClearPendingNoMatch();

      int price;

      if (!Plugin.Configuration.UndercutSelf && IsOwnRetainer(currentOfferings.ItemListings[i].RetainerId))
        price = (int)currentOfferings.ItemListings[i].PricePerUnit;
      else if (Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount)
        price = Math.Max((int)currentOfferings.ItemListings[i].PricePerUnit - Plugin.Configuration.UndercutAmount, 1);
      else
        price = Math.Max((100 - Plugin.Configuration.UndercutAmount) * (int)currentOfferings.ItemListings[i].PricePerUnit / 100, 1);

      CompletePriceRequest(price, currentOfferings.RequestId);
    }

    private void FrameworkOnUpdate(object _)
    {
      if (!_newRequest || _pendingNoMatchRequestId < 0 || Environment.TickCount64 < _pendingNoMatchTimeoutAt)
        return;

      Svc.Log.Debug("No matching market board listing received before timeout");
      CompletePriceRequest(-1, _pendingNoMatchRequestId);
    }

    private void ItemSearchResultPostSetup(AddonEvent type, AddonArgs args)
    {
      _newRequest = true;
      _useHq = Plugin.Configuration.HQ && _itemHq;
      ClearPendingNoMatch();
    }

    private unsafe void AddonRetainerSellPostSetup(AddonEvent type, AddonArgs args)
    {
      string nodeText = ((AddonRetainerSell*)args.Addon.Address)->ItemName->NodeText.ToString();
      _itemHq = nodeText.Contains('\uE03C');
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

    private static int GetNoMatchTimeoutMs() => Math.Max(Plugin.Configuration.MarketBoardKeepOpenMS, 1000);

    private void CompletePriceRequest(int price, int requestId)
    {
      ClearPendingNoMatch();
      _lastRequestId = requestId;
      _newRequest = false;
      NewPrice = price;
    }

    private void ClearPendingNoMatch()
    {
      _pendingNoMatchRequestId = -1;
      _pendingNoMatchTimeoutAt = 0;
    }
  }
}
