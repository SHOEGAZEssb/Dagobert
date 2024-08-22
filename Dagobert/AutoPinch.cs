using Dalamud.Interface.Utility;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Threading.Tasks;

namespace Dagobert
{
  internal class AutoPinch
  {
    private readonly MarketBoardHandler _mbHandler;
    private uint? _newPrice;
    private bool _pinching = false;

    public AutoPinch()
    {
      _mbHandler = new MarketBoardHandler();
      _mbHandler.NewPriceReceived += MBHandler_NewPriceReceived;
    }

    public async void Draw()
    {
      try
      {
        unsafe
        {
          var addon = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSellList");
          if (addon == null || !addon->AtkUnitBase.IsVisible)
            return;

          var node = addon->AtkUnitBase.UldManager.NodeList[17];

          if (node == null)
            return;

          var position = GetNodePosition(node);
          var scale = GetNodeScale(node);
          var size = new Vector2(node->Width, node->Height) * scale;

          ImGuiHelpers.ForceNextWindowMainViewport();
          var pos = position + size with { Y = 0 };
          pos.X -= size.X;
          ImGuiHelpers.SetNextWindowPosRelativeMainViewport(pos);


          ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
          var oldSize = ImGui.GetFont().Scale;
          ImGui.GetFont().Scale *= scale.X;
          ImGui.PushFont(ImGui.GetFont());
          ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
          ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f.Scale(), 3f.Scale()));
          ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
          ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
          ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
          ImGui.Begin($"###AutoPinch{node->NodeId}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
              | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
        }

        if (_pinching)
        {
          if (ImGui.Button("Cancel"))
          {
            _pinching = false;
          }
        }
        else
        {
          if (ImGui.Button("Auto Pinch"))
          {
            _pinching = true;
            await PinchAll();
            _pinching = false;
          }
        }
      }
      catch(Exception ex)
      {
        Svc.Log.Error(ex, "Error while auto pinching");
        _pinching = false;
      }
    }

    private async Task PinchAll()
    {
      int num = 0;

      if (Plugin.Configuration.ReopenRetainer)
      {
        ulong retainerId;
        unsafe
        {
          var rm = RetainerManager.Instance();
          var r = rm->GetActiveRetainer();
          retainerId = r->RetainerId;
        }

        await ReopenRetainerSellList(retainerId);
      }

      unsafe
      {
        var rm = RetainerManager.Instance();
        var r = rm->GetActiveRetainer();
        num = r->MarketItemCount;
      }

      for (int i = 0; i < num; i++)
      {
        if (!_pinching)
          return;

        Svc.Log.Info($"Pinching item #{i}");

        unsafe
        {
          var addon = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSellList");
          if (addon == null)
            throw new Exception($"Item #{i}: RetainerSellList is null");
          Callback.Fire(&addon->AtkUnitBase, false, 0, i, 1); // open context menu
                                                             // 0, 0, 1 -> open context menu, second 0 is item index
        }

        await Task.Delay(100);

        unsafe
        {
          var cm = (AddonContextMenu*)Svc.GameGui.GetAddonByName("ContextMenu");
          if (cm == null)
            throw new Exception($"Item #{i}: ContextMenu is null");
          Callback.Fire(&cm->AtkUnitBase, false, 0, 0, 0); // open retainersell
        }

        await Task.Delay(2500);

        unsafe
        {
          var retainerSell = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSell");
          if (retainerSell == null)
            throw new Exception($"Item #{i}: RetainerSell is null");
          Callback.Fire(&retainerSell->AtkUnitBase, false, 4); // open mb prices
        }

        await Task.Delay(500);

        await Task.Run(() =>
        {
          while (!_newPrice.HasValue)
          {
            Task.Delay(10);
          } // wait until price received
        });
        var p = _newPrice!.Value;
        _newPrice = null;

        unsafe
        {
          var itemSearchResult = (AddonItemSearchResult*)Svc.GameGui.GetAddonByName("ItemSearchResult");
          if (itemSearchResult == null)
            throw new Exception($"Item #{i}: ItemSearchResult is null");
          Callback.Fire(&itemSearchResult->AtkUnitBase, true, -1); // close itemsearchresult
        }

        await Task.Delay(100);

        unsafe
        {
          var retainerSell = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSell");
          if (retainerSell == null)
            throw new Exception($"Item #{i}: RetainerSell 2 is null");
          Callback.Fire(&retainerSell->AtkUnitBase, false, 2, (int)p); // input new price
          Callback.Fire(&retainerSell->AtkUnitBase, true, 0); // close retainersell
        }

        await Task.Delay(100);
      }
    }

    private async Task ReopenRetainerSellList(ulong retainerID)
    {
      unsafe
      {
        var retainerSellList = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSellList");
        if (retainerSellList == null)
          throw new Exception("Reopen Retainer: RetainerSellList is null");
        Callback.Fire(&retainerSellList->AtkUnitBase, true, -1); // close RetainerSellList
      }

      await Task.Delay(500);

      unsafe
      {
        var selectString = (AddonSelectString*)Svc.GameGui.GetAddonByName("SelectString");
        if (selectString == null)
          throw new Exception("Reopen Retainer: SelectString is null");
        Callback.Fire(&selectString->AtkUnitBase, true, 12); // close SelectString
      }

      await Task.Delay(500);

      unsafe
      {
        var talk = (AddonTalk*)Svc.GameGui.GetAddonByName("Talk");
        if (talk == null)
          throw new Exception("Reopen Retainer: Talk is null");
        Callback.Fire(&talk->AtkUnitBase, true, 1); // close Talk
      }

      await Task.Delay(1500);

      unsafe
      {
        var retainerList = (AddonRetainerList*)Svc.GameGui.GetAddonByName("RetainerList");
        if (retainerList == null)
          throw new Exception("Reopen Retainer: RetainerList is null");

        var rm = RetainerManager.Instance();
        uint i = 0;
        bool retainerFound = false;
        for (; i < rm->GetRetainerCount(); ++i)
        {
          if (rm->GetRetainerBySortedIndex(i)->RetainerId == retainerID)
          {
            retainerFound = true;
            break;
          }
        }

        if (retainerFound)
        {
          Svc.Log.Info($"Reopening retainer with id: {i}");
          Callback.Fire(&retainerList->AtkUnitBase, true, 2, i); // select Retainer
        }
        else
          throw new Exception("No valid retainer found to reopen");
      }

      await Task.Delay(1000);

      unsafe
      {
        var talk = (AddonTalk*)Svc.GameGui.GetAddonByName("Talk");
        if (talk == null)
          throw new Exception("Reopen Retainer: Talk 2 is null");
        Callback.Fire(&talk->AtkUnitBase, true, 1); // close Talk
      }

      await Task.Delay(500);

      unsafe
      {
        var selectString = (AddonSelectString*)Svc.GameGui.GetAddonByName("SelectString");
        if (selectString == null)
          throw new Exception("Reopen Retainer: SelectString is null");
        Callback.Fire(&selectString->AtkUnitBase, true, 2); // open sell
      }

      await Task.Delay(500);
    }

    private void MBHandler_NewPriceReceived(object? sender, NewPriceEventArgs e)
    {
      _newPrice = e.NewPrice;
    }

    public static unsafe Vector2 GetNodePosition(AtkResNode* node)
    {
      var pos = new Vector2(node->X, node->Y);
      var par = node->ParentNode;
      while (par != null)
      {
        pos *= new Vector2(par->ScaleX, par->ScaleY);
        pos += new Vector2(par->X, par->Y);
        par = par->ParentNode;
      }

      return pos;
    }

    public static unsafe Vector2 GetNodeScale(AtkResNode* node)
    {
      if (node == null) return new Vector2(1, 1);
      var scale = new Vector2(node->ScaleX, node->ScaleY);
      while (node->ParentNode != null)
      {
        node = node->ParentNode;
        scale *= new Vector2(node->ScaleX, node->ScaleY);
      }

      return scale;
    }
  }
}
