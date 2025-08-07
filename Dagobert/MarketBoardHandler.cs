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
    private readonly Lumina.Excel.ExcelSheet<Item> _items;
    private bool _newRequest;
    private bool _useHq;
    private bool _itemHq;
    private int _lastRequestId = -1;

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

      var i = 0;
      if (_useHq && _items.Single(j => j.RowId == currentOfferings.ItemListings[0].ItemId).CanBeHq)
      {
        while (i < currentOfferings.ItemListings.Count && !currentOfferings.ItemListings[i].IsHq)
          i++;
      }
      else
      {
        if (currentOfferings.ItemListings.Count > 0)
          i = 0;
        else
          i = currentOfferings.ItemListings.Count;
      }

      if (i >= currentOfferings.ItemListings.Count || currentOfferings.RequestId == _lastRequestId)
      {
        NewPrice = -1;
        return; // wait for more incoming offerings (currentOfferings only contains 10 per call)
      }
      else
      {
        int price;

        if (!Plugin.Configuration.UndercutSelf && IsOwnRetainer(currentOfferings.ItemListings[i].RetainerId))
          price = (int)currentOfferings.ItemListings[i].PricePerUnit;
        else if (Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount)
          price = Math.Max((int)currentOfferings.ItemListings[i].PricePerUnit - Plugin.Configuration.UndercutAmount, 1);
        else
          price = Math.Max((100 - Plugin.Configuration.UndercutAmount) * (int)currentOfferings.ItemListings[i].PricePerUnit / 100, 1);

        NewPrice = price;
      }

      _lastRequestId = currentOfferings.RequestId;
      _newRequest = false;
    }

    private void ItemSearchResultPostSetup(AddonEvent type, AddonArgs args)
    {
      _newRequest = true;
      _useHq = Plugin.Configuration.HQ && _itemHq;
    }

    private unsafe void AddonRetainerSellPostSetup(AddonEvent type, AddonArgs args)
    {
      string nodeText = ((AddonRetainerSell*)args.Addon.Address)->ItemName->NodeText.ToString();
      _itemHq = nodeText.Contains('\uE03C');
    }

    private unsafe bool IsOwnRetainer(ulong retainerId)
    {
      var retainerManager = RetainerManager.Instance();
      for (uint i = 0; i < retainerManager->GetRetainerCount(); ++i)
      {
        if (retainerId == retainerManager->GetRetainerBySortedIndex(i)->RetainerId)
          return true;
      }

      return false;
    }
  }
}
