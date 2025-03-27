using Dalamud.Game.Network.Structures;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dagobert
{
  internal unsafe class MarketBoardHandler : IDisposable
  {
    enum PennyPincherPacketType
    {
      MarketBoardItemRequestStart,
      MarketBoardOfferings
    }

    private delegate IntPtr AddonOnSetup(IntPtr addon, uint a2, IntPtr dataPtr);
    private readonly EzHook<AddonOnSetup> _retainerSellSetup;

    private unsafe delegate void* MarketBoardItemRequestStart(int* a1, int* a2, int* a3);
    private readonly EzHook<MarketBoardItemRequestStart> _marketBoardItemRequestStartHook;

    private readonly Lumina.Excel.ExcelSheet<Item> items;
    private bool newRequest;
    private bool useHq;
    private bool itemHq;

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
      items = Svc.Data.GetExcelSheet<Item>();

      Plugin.MarketBoard.OfferingsReceived += MarketBoardOnOfferingsReceived;

      _marketBoardItemRequestStartHook = new EzHook<MarketBoardItemRequestStart>("48 89 5C 24 ?? 57 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B FA E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 4A", MarketBoardItemRequestStartDetour);
      _marketBoardItemRequestStartHook.Enable();
      _retainerSellSetup = new EzHook<AddonOnSetup>("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 4C 89 74 24 ?? 49 8B F0 44 8B F2", AddonRetainerSell_OnSetup);
      _retainerSellSetup.Enable();
    }

    public void Dispose()
    {
      Plugin.MarketBoard.OfferingsReceived -= MarketBoardOnOfferingsReceived;
      _marketBoardItemRequestStartHook.Disable();
      _retainerSellSetup.Disable();
    }

    private void MarketBoardOnOfferingsReceived(IMarketBoardCurrentOfferings currentOfferings)
    {
      if (!newRequest)
        return;

      var i = 0;
      if (useHq && items.Single(j => j.RowId == currentOfferings.ItemListings[0].ItemId).CanBeHq)
      {
        while (i < currentOfferings.ItemListings.Count && (!currentOfferings.ItemListings[i].IsHq || IsOwnRetainer(currentOfferings.ItemListings[i].RetainerId)))
          i++;
      }
      else
      {
        while (i < currentOfferings.ItemListings.Count && IsOwnRetainer(currentOfferings.ItemListings[i].RetainerId))
          i++;
      }

      if (i == currentOfferings.ItemListings.Count)
        NewPrice = -1;
      else
      {
        var price = currentOfferings.ItemListings[i].PricePerUnit - 1;
        NewPrice = (int)price;
      }

      newRequest = false;
    }

    private unsafe void* MarketBoardItemRequestStartDetour(int* a1, int* a2, int* a3)
    {
      try
      {
        if (a3 != null)
          ParseNetworkEvent(PennyPincherPacketType.MarketBoardItemRequestStart);
      }
      catch (Exception e)
      {
        Svc.Log.Error(e, "Market board item request start detour crashed while parsing.");
      }

      return _marketBoardItemRequestStartHook!.Original(a1, a2, a3);
    }

    private void ParseNetworkEvent(PennyPincherPacketType packetType)
    {
      if (packetType == PennyPincherPacketType.MarketBoardItemRequestStart)
      {
        newRequest = true;
        useHq = Plugin.Configuration.HQ && itemHq;
      }
    }

    private unsafe IntPtr AddonRetainerSell_OnSetup(IntPtr addon, uint a2, IntPtr dataPtr)
    {
      var result = _retainerSellSetup.Original(addon, a2, dataPtr);

      string nodeText = ((AddonRetainerSell*)addon)->ItemName->NodeText.ToString();
      itemHq = nodeText.Contains('\uE03C');

      return result;
    }

    private unsafe bool IsOwnRetainer(ulong retainerId)
    {
      var retainerManager = RetainerManager.Instance();
      for (uint i = 0; i < retainerManager->GetRetainerCount(); ++i)
      {
        if (retainerId == retainerManager->GetRetainerBySortedIndex(i)->RetainerId)
        {
          return true;
        }
      }

      return false;
    }
  }
}
