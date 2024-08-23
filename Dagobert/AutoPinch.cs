using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Dagobert
{
  internal class AutoPinch : IDisposable
  {
    private readonly MarketBoardHandler _mbHandler;
    private uint? _newPrice;
    private bool _shouldPinch = false;
    private bool _currentlyPinching = false;
    private readonly TaskManager _taskManager = new TaskManager();

    public AutoPinch()
    {
      _mbHandler = new MarketBoardHandler();
      _mbHandler.NewPriceReceived += MBHandler_NewPriceReceived;
    }

    public void Dispose()
    {
      _mbHandler.NewPriceReceived -= MBHandler_NewPriceReceived;
      _mbHandler.Dispose();
    }

    public void Draw()
    {
      try
      {
        float oldSize = 0;
        unsafe
        {
          var addon = (AddonRetainerSell*)Svc.GameGui.GetAddonByName("RetainerSellList");
          if (addon == null || !addon->AtkUnitBase.IsVisible || !addon->IsReady)
            return;

          var node = addon->AtkUnitBase.UldManager.NodeList[17];

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
        }

        DrawAutoPinchButton();

        ImGui.End();
        ImGui.PopStyleVar(5);
        ImGui.GetFont().Scale = oldSize;
        ImGui.PopFont();
        ImGui.PopStyleColor();
      }
      catch (Exception ex)
      {
        _shouldPinch = false;
        _currentlyPinching = false;
        _taskManager.Abort();
        Svc.Log.Error(ex, "Error while auto pinching");
        Svc.Chat.PrintError($"Error while auto pinching: {ex.Message}");
      }
    }

    private void DrawAutoPinchButton()
    {
      if (_shouldPinch)
      {
        if (ImGui.Button("Cancel"))
          _shouldPinch = false;
        if (ImGui.IsItemHovered())
        {
          ImGui.BeginTooltip();
          ImGui.SetTooltip("Cancels the auto pinching process after the current item");
          ImGui.EndTooltip();
        }
      }
      else
      {
        bool disabled = _currentlyPinching || _shouldPinch;
        if (disabled)
          ImGui.BeginDisabled();

        if (ImGui.Button("Auto Pinch"))
          _taskManager.Enqueue(() => _ = PinchAll());
        if (ImGui.IsItemHovered())
        {
          string tooltipText;
          if (disabled)
            tooltipText = "Canceling auto pinch after current item...";
          else
          {
            tooltipText = "Starts auto pinching\r\n" +
                          "Please do not interact with the game while this process is running";
          }

          ImGui.BeginTooltip();
          ImGui.SetTooltip(tooltipText);
          ImGui.EndTooltip();
        }

        if (disabled)
          ImGui.EndDisabled();
      }
    }

    private async Task PinchAll()
    {
      _currentlyPinching = true;
      _shouldPinch = true;

      int num = 0;
      var retainerSellList = await WaitForAddon("RetainerSellList");
      unsafe
      {
        var rsl = (AddonRetainerSell*)retainerSellList;
        if (rsl == null)
          throw new Exception("RetainerSellList is null");
        var listNode = (AtkComponentNode*)rsl->AtkUnitBase.UldManager.NodeList[10];
        var listComponent = (AtkComponentList*)listNode->Component;
        num = listComponent->ListLength;
      }

      for (int i = 0; i < num; i++)
      {
        if (!_shouldPinch)
        {
          _currentlyPinching = false;
          return;
        }

        Svc.Log.Info($"Pinching item #{i}");
        
        unsafe
        {
          var addon = (AddonRetainerSell*)retainerSellList;
          var listNode = (AtkComponentNode*)addon->AtkUnitBase.UldManager.NodeList[10];
          var listComponent = (AtkComponentList*)listNode->Component;
          listComponent->SelectItem(i, true);
          Callback.Fire(&addon->AtkUnitBase, false, 0, i, 1); // open context menu
                                                              // 0, 0, 1 -> open context menu, second 0 is item index
        }

        await Task.Delay(50);

        var contextMenu = await WaitForAddon("ContextMenu");
        unsafe
        {
          var cm = (AddonContextMenu*)contextMenu;
          if (cm == null)
            throw new Exception($"Item #{i}: ContextMenu is null");
          Callback.Fire(&cm->AtkUnitBase, true, 0, 0, 0); // open retainersell
        }

        await Task.Delay(TimeSpan.FromMilliseconds(Plugin.Configuration.GetMBPricesDelayMS)); // market board rate limiting delay

        var retainerSell = await WaitForAddon("RetainerSell");
        unsafe
        {
          var rs = (AddonRetainerSell*)retainerSell;
          if (rs == null)
            throw new Exception($"Item #{i}: RetainerSell is null");
          Callback.Fire(&rs->AtkUnitBase, true, 4); // open mb prices
        }

        await Task.Run(() =>
        {
          while (!_newPrice.HasValue)
          {
            Task.Delay(10);
          } // wait until price received
        });
        var p = _newPrice!.Value;
        _newPrice = null;

        await Task.Delay(50);

        var itemSearchResult = await WaitForAddon("ItemSearchResult");
        unsafe
        {
          var isr = (AddonItemSearchResult*)itemSearchResult;
          if (isr == null)
            throw new Exception($"Item #{i}: ItemSearchResult is null");
          Callback.Fire(&isr->AtkUnitBase, true, -1); // close itemsearchresult
        }

        await Task.Delay(50);

        unsafe
        {
          var rs = (AddonRetainerSell*)retainerSell;
          Callback.Fire(&rs->AtkUnitBase, true, 2, (int)p); // input new price       
        }

        await Task.Delay(100);

        unsafe
        {
          var rs = (AddonRetainerSell*)retainerSell;
          Callback.Fire(&rs->AtkUnitBase, true, 0); // close retainersell
        }

        await Task.Delay(100);
      }

      _shouldPinch = false;
      _currentlyPinching = false;
      Svc.Chat.Print("Auto pinching was successfull");
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

    private static async Task<nint> WaitForAddon(string addonName)
    {
      return await WaitForAddon(addonName, TimeSpan.FromMilliseconds(Plugin.Configuration.GetAddonMaxTimeoutMS));
    }

    private static async Task<nint> WaitForAddon(string addonName, TimeSpan timeout)
    {
      using CancellationTokenSource cts = new();
      var tryGetAddonTask = TryGetAddon(addonName, cts.Token);
      var completedTask = await Task.WhenAny(tryGetAddonTask, Task.Delay(timeout));

      if (completedTask == tryGetAddonTask)
        return tryGetAddonTask.Result;
      else
      {
        cts.Cancel();
        return nint.Zero;
      }
    }

    private static async Task<nint> TryGetAddon(string addonName, CancellationToken token)
    {
      nint addon = 0;
      while (addon == nint.Zero && !token.IsCancellationRequested)
      {
        unsafe
        {
          var addonTemp = (AtkUnitBase*)Svc.GameGui.GetAddonByName(addonName);
          if (addonTemp != null && addonTemp->IsReady && addonTemp->IsVisible)
            addon = (nint)addonTemp;

        }
        await Task.Delay(50, CancellationToken.None);
      }

      return addon;
    }
  }
}