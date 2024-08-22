using Dalamud.Game.Network.Structures;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dagobert
{
  internal unsafe class MarketBoardHandler
  {
    enum PennyPincherPacketType
    {
      MarketBoardItemRequestStart,
      MarketBoardOfferings
    }

    private delegate IntPtr GetFilePointer(byte index);
    [Signature("E8 ?? ?? ?? ?? 48 85 C0 74 14 83 7B 44 00")]
    private readonly GetFilePointer getFilePtr;

    private delegate IntPtr AddonOnSetup(IntPtr addon, uint a2, IntPtr dataPtr);
    private readonly EzHook<AddonOnSetup> _retainerSellSetup;

    private unsafe delegate void* MarketBoardItemRequestStart(int* a1, int* a2, int* a3);
    private readonly EzHook<MarketBoardItemRequestStart> _marketBoardItemRequestStartHook;

    private readonly Lumina.Excel.ExcelSheet<Item> items;
    private bool newRequest;
    private bool useHq;
    private bool itemHq;

    public uint NewPrice
    {
      get => _newPrice;
      private set
      {
        _newPrice = value;
        NewPriceReceived?.Invoke(this, new NewPriceEventArgs(NewPrice));
      }
    }
    private uint _newPrice;

    public event EventHandler<NewPriceEventArgs> NewPriceReceived;

    public MarketBoardHandler()
    {
      items = Svc.Data.GetExcelSheet<Item>();

      Plugin.MarketBoard.OfferingsReceived += MarketBoardOnOfferingsReceived;

      _marketBoardItemRequestStartHook = new EzHook<MarketBoardItemRequestStart>("48 89 5C 24 ?? 57 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 48 8B FA E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 4A", MarketBoardItemRequestStartDetour);
      _marketBoardItemRequestStartHook.Enable();
      _retainerSellSetup = new EzHook<AddonOnSetup>("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 4C 89 74 24 ?? 49 8B F0 44 8B F2", AddonRetainerSell_OnSetup);
      _retainerSellSetup.Enable();
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

      if (i == currentOfferings.ItemListings.Count) return;

      var price = currentOfferings.ItemListings[i].PricePerUnit - 1;

      NewPrice = price;
      newRequest = false;
    }

    private unsafe void* MarketBoardItemRequestStartDetour(int* a1, int* a2, int* a3)
    {
      try
      {
        if (a3 != null)
        {
          var ptr = (IntPtr)a2;
          ParseNetworkEvent(ptr, PennyPincherPacketType.MarketBoardItemRequestStart);
        }
      }
      catch (Exception e)
      {
        Svc.Log.Error(e, "Market board item request start detour crashed while parsing.");
      }

      return _marketBoardItemRequestStartHook!.Original(a1, a2, a3);
    }

    private void ParseNetworkEvent(IntPtr dataPtr, PennyPincherPacketType packetType)
    {
      // if (!Data.IsDataReady) return;
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

    private bool Retainer()
    {
      return (getFilePtr != null) && Marshal.ReadInt64(getFilePtr(7), 0xB0) != 0;
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
