using Dalamud.Game.ClientState.Keys;
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

    public async void Draw()
    {
      try
      {
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

        await DrawAutoPinchButton();
      }
      catch (Exception ex)
      {
        _shouldPinch = false;
        _currentlyPinching = false;
        Svc.Log.Error(ex, "Error while auto pinching");
        Svc.Chat.PrintError($"Error while auto pinching: {ex.Message}");
      }
    }

    private async Task DrawAutoPinchButton()
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
        {
          _currentlyPinching = true;
          _shouldPinch = true;
          await PinchAll();
          _shouldPinch = false;
          _currentlyPinching = false;
          Svc.Chat.Print("Auto pinching was successfull");
        }
        if (ImGui.IsItemHovered())
        {
          string tooltipText;
          if (disabled)
            tooltipText = "Canceling auto pinch...";
          else
          {
            tooltipText = "Starts auto pinching\r\n";
            var shiftHeld = Plugin.KeyState[VirtualKey.SHIFT];
            if ((Plugin.Configuration.ShiftBehaviour == ShiftBehaviour.ReopenRetainer && shiftHeld) ||
                (Plugin.Configuration.ShiftBehaviour == ShiftBehaviour.DontReopenRetainer && !shiftHeld))
              tooltipText += "Retainer will be reopened";
            else
              tooltipText += "Retainer will not be reopened";
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
      int num = 0;

      var shiftHeld = Plugin.KeyState[VirtualKey.SHIFT];
      if ((Plugin.Configuration.ShiftBehaviour == ShiftBehaviour.ReopenRetainer && shiftHeld)
        || (Plugin.Configuration.ShiftBehaviour == ShiftBehaviour.DontReopenRetainer && !shiftHeld))
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
        if (!_shouldPinch)
          return;

        Svc.Log.Info($"Pinching item #{i}");

        var retainerSellList = await WaitForAddon("RetainerSellList");
        unsafe
        {
          var addon = (AddonRetainerSell*)retainerSellList;
          if (addon == null)
            throw new Exception($"Item #{i}: RetainerSellList is null");
          Callback.Fire(&addon->AtkUnitBase, false, 0, i, 1); // open context menu
                                                              // 0, 0, 1 -> open context menu, second 0 is item index
        }

        var contextMenu = await WaitForAddon("ContextMenu");
        unsafe
        {
          var cm = (AddonContextMenu*)contextMenu;
          if (cm == null)
            throw new Exception($"Item #{i}: ContextMenu is null");
          Callback.Fire(&cm->AtkUnitBase, false, 0, 0, 0); // open retainersell
        }

        await Task.Delay(2500); // market board rate limiting delay

        var retainerSell = await WaitForAddon("RetainerSell");
        unsafe
        {
          var rs = (AddonRetainerSell*)retainerSell;
          if (rs == null)
            throw new Exception($"Item #{i}: RetainerSell is null");
          Callback.Fire(&rs->AtkUnitBase, false, 4); // open mb prices
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

        var itemSearchResult = await WaitForAddon("ItemSearchResult");
        unsafe
        {
          var isr = (AddonItemSearchResult*)itemSearchResult;
          if (isr == null)
            throw new Exception($"Item #{i}: ItemSearchResult is null");
          Callback.Fire(&isr->AtkUnitBase, true, -1); // close itemsearchresult
        }

        var retainerSell2 = await WaitForAddon("RetainerSell");
        unsafe
        {
          var rs = (AddonRetainerSell*)retainerSell2;
          if (rs == null)
            throw new Exception($"Item #{i}: RetainerSell 2 is null");
          Callback.Fire(&rs->AtkUnitBase, false, 2, (int)p); // input new price
          Callback.Fire(&rs->AtkUnitBase, true, 0); // close retainersell
        }
      }
    }

    private async Task ReopenRetainerSellList(ulong retainerID)
    {
      var retainerSellList = await WaitForAddon("RetainerSellList");
      unsafe
      {
        var rsl = (AddonRetainerSell*)retainerSellList;
        if (rsl == null)
          throw new Exception("Reopen Retainer: RetainerSellList is null");
        Callback.Fire(&rsl->AtkUnitBase, true, -1); // close RetainerSellList
      }

      var selectString = await WaitForAddon("SelectString");
      unsafe
      {
        var ss = (AddonSelectString*)selectString;
        if (ss == null)
          throw new Exception("Reopen Retainer: SelectString is null");
        Callback.Fire(&ss->AtkUnitBase, true, 12); // close SelectString
      }

      var talk = await WaitForAddon("Talk");
      unsafe
      {
        var t = (AddonTalk*)talk;
        if (t == null)
          throw new Exception("Reopen Retainer: Talk is null");
        Callback.Fire(&t->AtkUnitBase, true, 1); // close Talk
      }

      var retainerList = await WaitForAddon("RetainerList");
      unsafe
      {
        var rs = (AddonRetainerList*)retainerList;
        if (rs == null)
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
          Callback.Fire(&rs->AtkUnitBase, true, 2, i); // select Retainer
        }
        else
          throw new Exception("No valid retainer found to reopen");
      }

      var talk2 = await WaitForAddon("Talk");
      unsafe
      {
        var t = (AddonTalk*)talk2;
        if (t == null)
          throw new Exception("Reopen Retainer: Talk 2 is null");
        Callback.Fire(&t->AtkUnitBase, true, 1); // close Talk
      }

      var selectString2 = await WaitForAddon("SelectString");
      unsafe
      {
        var ss = (AddonSelectString*)selectString2;
        if (ss == null)
          throw new Exception("Reopen Retainer: SelectString is null");
        Callback.Fire(&ss->AtkUnitBase, true, 2); // open sell
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
        await Task.Delay(10, CancellationToken.None);
      }

      return addon;
    }
  }
}