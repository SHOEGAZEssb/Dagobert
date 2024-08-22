using Dalamud.Interface.Utility;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dagobert
{
  internal class AutoPinch
  {
    private MarketBoardHandler _mbHandler;
    private uint? _newPrice;

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

          var node = addon->AtkUnitBase.UldManager.NodeList[12];

          if (node == null)
            return;

          var position = GetNodePosition(node);
          var scale = GetNodeScale(node);
          var size = new Vector2(node->Width, node->Height) * scale;

          ImGuiHelpers.ForceNextWindowMainViewport();
          var pos = position + size with { Y = 0 };
          pos.X += 12f;
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
          ImGui.Begin($"###DesynthAll{node->NodeId}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
              | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);
        }

        if (ImGui.Button("Test"))
        {
          await PinchAll();
        }
      }
      catch
      {

      }
    }

    private async Task PinchAll()
    {
      int num = 0;
      unsafe
      {
        var rm = RetainerManager.Instance();
        var r = rm->GetActiveRetainer();
        num = r->MarketItemCount;
      }

      for (int i = 0; i < num; i++)
      {
        Svc.Log.Info($"Pinching item #{i}");

        unsafe
        {
          var addon = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSellList");
          Callback.Fire(&addon->AtkUnitBase, true, 0, i, 1); // open context menu
                                                             // 0, 0, 1 -> open context menu, second 0 is item index
        }

        await Task.Delay(100);

        unsafe
        {
          var cm = (AddonContextMenu*)Svc.GameGui.GetAddonByName("ContextMenu");
          Callback.Fire(&cm->AtkUnitBase, true, 0, 0, 0); // open retainersell
        }

        await Task.Delay(2500);

        unsafe
        {
          var retainerSell = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSell");
          Callback.Fire(&retainerSell->AtkUnitBase, true, 4); // open mb prices
        }

        await Task.Delay(500);

        await Task.Run(() =>
        {
          while (!_newPrice.HasValue)
          {
            Task.Delay(10);
          } // wait until price received
        });
        var p = _newPrice.Value;
        _newPrice = null;

        unsafe
        {
          var itemSearchResult = (AddonItemSearchResult*)Svc.GameGui.GetAddonByName("ItemSearchResult");
          Callback.Fire(&itemSearchResult->AtkUnitBase, true, -1); // close itemsearchresult
        }

        await Task.Delay(100);

        unsafe
        { 
          var retainerSell = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSell");
          Callback.Fire(&retainerSell->AtkUnitBase, true, 2, (int)p); // input new price
          Callback.Fire(&retainerSell->AtkUnitBase, true, 0); // close retainersell
        }

        await Task.Delay(100);
      }
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
