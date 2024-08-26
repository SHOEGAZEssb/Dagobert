using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Dagobert
{
  internal class AutoPinch : Window, IDisposable
  {
    private readonly MarketBoardHandler _mbHandler;
    private int? _newPrice;
    private readonly TaskManager _taskManager;

    public AutoPinch()
      : base("Dagobert", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
      _mbHandler = new MarketBoardHandler();
      _mbHandler.NewPriceReceived += MBHandler_NewPriceReceived;

      // window
      Position = new System.Numerics.Vector2(0, 0);
      IsOpen = true;
      ShowCloseButton = false;
      RespectCloseHotkey = false;
      DisableWindowSounds = true;
      SizeConstraints = new WindowSizeConstraints()
      {
        MaximumSize = new System.Numerics.Vector2(0, 0),
      };

      _taskManager = new TaskManager
      {
        TimeLimitMS = 10000,
        AbortOnTimeout = true
      };
    }

    public void Dispose()
    {
      _mbHandler.NewPriceReceived -= MBHandler_NewPriceReceived;
      _mbHandler.Dispose();
    }

    public override void Draw()
    {
      try
      {
        float oldSize = 0;
        unsafe
        {
          if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
          {

            var node = addon->UldManager.NodeList[17];

            if (node == null)
              return;

            var position = GetNodePosition(node);
            var scale = GetNodeScale(node);
            var size = new Vector2(node->Width, node->Height) * scale;

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
            oldSize = ImGui.GetFont().Scale;
            ImGui.GetFont().Scale *= scale.X;
            ImGui.PushFont(ImGui.GetFont());
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f.Scale(), 3f.Scale()));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
            ImGui.Begin($"###AutoPinch{node->NodeId}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

            DrawAutoPinchButton();

            ImGui.End();
            ImGui.PopStyleVar(5);
            ImGui.GetFont().Scale = oldSize;
            ImGui.PopFont();
            ImGui.PopStyleColor();
          }
        }
      }
      catch (Exception ex)
      {
        _taskManager.Abort();
        Svc.Log.Error(ex, "Error while auto pinching");
        Svc.Chat.PrintError($"Error while auto pinching: {ex.Message}");
      }
    }

    private void DrawAutoPinchButton()
    {
      if (_taskManager.IsBusy)
      {
        if (ImGui.Button("Cancel"))
          _taskManager.Abort();
        if (ImGui.IsItemHovered())
        {
          ImGui.BeginTooltip();
          ImGui.SetTooltip("Cancels the auto pinching process");
          ImGui.EndTooltip();
        }
      }
      else
      {
        if (ImGui.Button("Auto Pinch"))
          PinchAll();
        if (ImGui.IsItemHovered())
        {
          ImGui.BeginTooltip();
          ImGui.SetTooltip("Starts auto pinching\r\n" +
                           "Please do not interact with the game while this process is running");
          ImGui.EndTooltip();
        }

      }
    }

    private unsafe void PinchAll()
    {
      _newPrice = null;
      if (_taskManager.IsBusy)
        return;

      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        var listNode = (AtkComponentNode*)addon->UldManager.NodeList[10];
        var listComponent = (AtkComponentList*)listNode->Component;
        int num = listComponent->ListLength;
        for (int i = 0; i < num; i++)
        {
          EnqueueSingleItem(i);
        }
      }
    }

    private void EnqueueSingleItem(int index)
    {
      _taskManager.Enqueue(() => OpenItemContextMenu(index), "OpenItemContextMenu");
      _taskManager.DelayNext(100);
      _taskManager.Enqueue(ClickAdjustPrice, "ClickAdjustPrice");
      _taskManager.DelayNext(Plugin.Configuration.GetMBPricesDelayMS);
      _taskManager.Enqueue(ClickComparePrice, "ClickComparePrice");
      _taskManager.DelayNext(1000);
      _taskManager.Enqueue(SetNewPrice, "SetNewPrice");
    }

    private static unsafe bool? OpenItemContextMenu(int itemIndex)
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        Svc.Log.Debug($"Clicking item {itemIndex}");
        Callback.Fire(addon, true, 0, itemIndex, 1); // click item
        return true;
      }

      return false;
    }

    private static unsafe bool? ClickAdjustPrice()
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        Svc.Log.Debug($"Clicking adjust price");
        Callback.Fire(addon, true, 0, 0, 0, 0, 0); // click adjust price
        return true;
      }

      return false;
    }

    private static unsafe bool? ClickComparePrice()
    {
      if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
      {
        Svc.Log.Debug($"Clicking compare prices");
        Callback.Fire(&addon->AtkUnitBase, true, 4);
        return true;
      }

      return false;
    }

    private unsafe bool? SetNewPrice()
    {
      try
      {
        // close compare price window
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out var addon))
          addon->Close(true);
        else
          return false;

        if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var retainerSell) && GenericHelpers.IsAddonReady(&retainerSell->AtkUnitBase))
        {
          Svc.Log.Debug($"Setting new price");
          var ui = &retainerSell->AtkUnitBase;
          if (!_newPrice.HasValue || _newPrice <= 0)
          {
            Callback.Fire(&retainerSell->AtkUnitBase, true, 1); // cancel
            ui->Close(true);
            return false;
          }
          else
          {
            retainerSell->AskingPrice->SetValue(_newPrice!.Value);
            Callback.Fire(&retainerSell->AtkUnitBase, true, 0); // confirm
            ui->Close(true);
            return true;
          }
        }
        else
          return false;
      }
      finally
      {
        _newPrice = null;
      }
    }

    private void MBHandler_NewPriceReceived(object? sender, NewPriceEventArgs e)
    {
      Svc.Log.Debug($"New price received: {e.NewPrice}");
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