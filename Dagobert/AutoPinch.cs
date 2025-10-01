using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.UIHelpers.AddonMasterImplementations;
using ECommons.UIHelpers.AtkReaderImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using static ECommons.UIHelpers.AtkReaderImplementations.ReaderContextMenu;

namespace Dagobert
{
  internal sealed class AutoPinch : Window, IDisposable
  {
    private readonly MarketBoardHandler _mbHandler;
    private int? _newPrice;
    private bool _skipCurrentItem = false;
    private readonly TaskManager _taskManager;
    private Dictionary<string, int?> _cachedPrices = [];

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
      // Fails on non-windows
      try
      {
        var tts = new SpeechSynthesizer();
        tts.SelectVoice(tts.Voice.Name);
        Plugin.Configuration.DontUseTTS = false;
        Plugin.Configuration.Save();
      }
      catch
      {
        Plugin.Configuration.DontUseTTS = true;
        Plugin.Configuration.Save();
      }

      Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, RetainerSellPostSetup);
    }

    public void Dispose()
    {
      Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, RetainerSellPostSetup);
      _mbHandler.NewPriceReceived -= MBHandler_NewPriceReceived;
      _mbHandler.Dispose();
    }

    public override void Draw()
    {
      try
      {
        DrawForRetainerList();
        DrawForRetainerSellList();
      }
      catch (Exception ex)
      {
        _taskManager.Abort();
        Svc.Log.Error(ex, "Error while auto pinching");
        if (Plugin.Configuration.ShowErrorsInChat)
          Svc.Chat.PrintError($"Error while auto pinching: {ex.Message}");

        RemoveTalkAddonListeners();
      }
    }

    private void DrawForRetainerList()
    {
      unsafe
      {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
          if (Plugin.Configuration.EnablePinchKey && Plugin.KeyState[Plugin.Configuration.PinchKey])
            PinchAllRetainerItems();

          var node = addon->UldManager.NodeList[27];

          if (node == null)
            return;

          var oldSize = ImGuiSetup(node);
          DrawAutoPinchButton(PinchAllRetainers);
          ImGuiPostSetup(oldSize);
        }
      }
    }

    private void DrawForRetainerSellList()
    {
      unsafe
      {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
        {
          if (Plugin.Configuration.EnablePinchKey && Plugin.KeyState[Plugin.Configuration.PinchKey])
            PinchAllRetainerItems();

          var node = addon->UldManager.NodeList[17];

          if (node == null)
            return;

          var oldSize = ImGuiSetup(node);
          DrawAutoPinchButton(PinchAllRetainerItems);
          ImGuiPostSetup(oldSize);
        }
      }
    }

    private unsafe float ImGuiSetup(AtkResNode* node)
    {
      var position = GetNodePosition(node);
      var scale = GetNodeScale(node);
      var size = new Vector2(node->Width, node->Height) * scale;

      ImGuiHelpers.ForceNextWindowMainViewport();
      ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

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

      return oldSize;
    }

    private static void ImGuiPostSetup(float oldSize)
    {
      ImGui.End();
      ImGui.PopStyleVar(5);
      ImGui.GetFont().Scale = oldSize;
      ImGui.PopFont();
      ImGui.PopStyleColor();
    }

    private void DrawAutoPinchButton(Action specificPinchFunction)
    {
      if (_taskManager.IsBusy)
      {
        if (ImGui.Button("Cancel"))
        {
          _taskManager.Abort();
          RemoveTalkAddonListeners();
        }
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
          specificPinchFunction();
        if (ImGui.IsItemHovered())
        {
          ImGui.BeginTooltip();
          ImGui.SetTooltip("Starts auto pinching\r\n" +
                           "Please do not interact with the game while this process is running");
          ImGui.EndTooltip();
        }
      }
    }

    private unsafe void PinchAllRetainers()
    {
      if (_taskManager.IsBusy)
        return;

      ClearState();
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Talk", SkipRetainerDialog);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "Talk", SkipRetainerDialog);

        // we cache the number of retainers because AddonMaster will be disposed once the RetainerList addon is closed.
        var num = new AddonMaster.RetainerList(addon).Retainers.Length;
        for (int i = 0; i < num; i++)
        {
          EnqueueSingleRetainer(i);
        }

        _taskManager.Enqueue(RemoveTalkAddonListeners);
        if (Plugin.Configuration.TTSWhenAllDone)
          _taskManager.Enqueue(() => SpeakTTS("Finished auto pinching all retainers"), $"SpeakTTSAll");
      }
    }

    private void EnqueueSingleRetainer(int index)
    {
      _taskManager.Enqueue(() => ClickRetainer(index), $"ClickRetainer{index}");
      _taskManager.DelayNext(100);
      _taskManager.Enqueue(ClickSellItems, $"ClickSellItems{index}");
      _taskManager.DelayNext(500);
      _taskManager.Enqueue(() => EnqueueAllRetainerItems(InsertSingleItem, true), $"EnqueueAllRetainerItems{index}");
      _taskManager.DelayNext(500);
      _taskManager.Enqueue(CloseRetainerSellList, $"CloseRetainerSellList{index}");
      _taskManager.DelayNext(100);
      _taskManager.Enqueue(CloseRetainer, $"CloseRetainer{index}");
      _taskManager.DelayNext(100);
    }

    private static unsafe bool? ClickRetainer(int index)
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        ECommons.Automation.Callback.Fire(addon, true, 2, index);
        return true;
      }
      else
        return false;
    }

    private static unsafe bool? ClickSellItems()
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        new AddonMaster.SelectString(addon).Entries[2].Select();
        return true;
      }
      else
        return false;
    }

    private static unsafe bool? CloseRetainerSellList()
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        addon->Close(true);
        return true;
      }
      else
        return false;
    }

    private static unsafe bool? CloseRetainer()
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectString", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        addon->Close(true);
        return true;
      }
      else
        return false;
    }

    private unsafe void PinchAllRetainerItems()
    {
      if (_taskManager.IsBusy)
        return;

      ClearState();
      EnqueueAllRetainerItems(EnqueueSingleItem, false);
    }

    private unsafe bool? EnqueueAllRetainerItems(Action<int> enqueueFunc, bool reverseOrder)
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        var listNode = (AtkComponentNode*)addon->UldManager.NodeList[10];
        var listComponent = (AtkComponentList*)listNode->Component;
        int num = listComponent->ListLength;
        if (reverseOrder)
        {
          for (int i = num - 1; i >= 0; i--)
          {
            enqueueFunc(i);
          }
        }
        else
        {
          for (int i = 0; i < num; i++)
          {
            enqueueFunc(i);
          }
        }
        if (Plugin.Configuration.TTSWhenEachDone)
          _taskManager.Enqueue(() => SpeakTTS("Auto Pinch done"), $"SpeakTTSEach");

        return true;
      }
      else
        return false;
    }

    private void EnqueueSingleItem(int index)
    {
      _taskManager.Enqueue(() => OpenItemContextMenu(index), $"OpenItemContextMenu{index}");
      _taskManager.DelayNext(100);
      _taskManager.Enqueue(ClickAdjustPrice, $"ClickAdjustPrice{index}");
      _taskManager.DelayNext(100);
      _taskManager.Enqueue(DelayMarketBoard, $"DelayMB{index}");
      _taskManager.Enqueue(ClickComparePrice, $"ClickComparePrice{index}");
      _taskManager.DelayNext(Plugin.Configuration.MarketBoardKeepOpenMS);
      _taskManager.Enqueue(SetNewPrice, $"SetNewPrice{index}");
    }

    private void InsertSingleItem(int index)
    {
      // reverse order because we INSERT
      _taskManager.Insert(SetNewPrice, $"SetNewPrice{index}");
      _taskManager.InsertDelayNext(Plugin.Configuration.MarketBoardKeepOpenMS);
      _taskManager.Insert(ClickComparePrice, $"ClickComparePrice{index}");
      _taskManager.Insert(DelayMarketBoard, $"DelayMB{index}");
      _taskManager.InsertDelayNext(100);
      _taskManager.Insert(ClickAdjustPrice, $"ClickAdjustPrice{index}");
      _taskManager.InsertDelayNext(100);
      _taskManager.Insert(() => OpenItemContextMenu(index), $"OpenItemContextMenu{index}");
    }

    private static unsafe bool? OpenItemContextMenu(int itemIndex)
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("RetainerSellList", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        Svc.Log.Debug($"Clicking item {itemIndex}");
        ECommons.Automation.Callback.Fire(addon, true, 0, itemIndex, 1); // click item
        return true;
      }

      return false;
    }

    private unsafe bool? ClickAdjustPrice()
    {
      if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ContextMenu", out var addon) && GenericHelpers.IsAddonReady(addon))
      {
        var reader = new ReaderContextMenu(addon);
        if (!IsItemMannequin(reader.Entries))
        {
          Svc.Log.Debug($"Clicking adjust price");
          ECommons.Automation.Callback.Fire(addon, true, 0, 0, 0, 0, 0); // click adjust price
        }
        else
        {
          Svc.Log.Debug("Current item is a mannequin item and will be skipped");
          _skipCurrentItem = true;
          addon->Close(true);
        }

        return true;
      }

      return false;
    }

    /// <summary>
    /// Checks if an item is a mannequin item, by checking if there is
    /// the "adjust price" entry in the given <paramref name="contextMenuEntries"/>.
    /// </summary>
    /// <param name="contextMenuEntries">Context menu entries to check.</param>
    /// <returns>True if item is a mannequin item, false otherwise.</returns>
    private static bool IsItemMannequin(List<ContextMenuEntry> contextMenuEntries)
    {
      return !contextMenuEntries.Any((e) => e.Name.Equals("adjust price", StringComparison.CurrentCultureIgnoreCase)
                                        || e.Name.Equals("preis ändern", StringComparison.CurrentCultureIgnoreCase)
                                        || e.Name.Equals("価格を変更する", StringComparison.CurrentCultureIgnoreCase)
                                        || e.Name.Equals("changer le prix", StringComparison.CurrentCultureIgnoreCase));
    }

    private unsafe bool? DelayMarketBoard()
    {
      if (_skipCurrentItem)
        return true;

      if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
      {
        var itemName = addon->ItemName->NodeText.ToString();
        if (!_cachedPrices.TryGetValue(itemName, out int? value) || value <= 0)
        {
          Svc.Log.Debug($"{itemName} has no cached price (or that price was <= 0), delaying next mb open");
          _taskManager.InsertDelayNext(Plugin.Configuration.GetMBPricesDelayMS);
        }

        return true;
      }

      return false;
    }

    private unsafe bool? ClickComparePrice()
    {
      if (_skipCurrentItem)
        return true;

      if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
      {
        // if we have a cached price, dont click compare
        var itemName = addon->ItemName->NodeText.ToString();
        if (_cachedPrices.TryGetValue(itemName, out int? value) && value > 0)
        {
          Svc.Log.Debug($"{itemName}: using cached price");
          _newPrice = value;
          return true;
        }
        else
        {
          Svc.Log.Debug($"Clicking compare prices");
          ECommons.Automation.Callback.Fire(&addon->AtkUnitBase, true, 4);
          return true;
        }
      }

      return false;
    }

    private unsafe bool? SetNewPrice()
    {
      try
      {
        if (_skipCurrentItem)
          return true;

        // close compare price window
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("ItemSearchResult", out var addon))
          addon->Close(true);

        if (GenericHelpers.TryGetAddonByName<AddonRetainerSell>("RetainerSell", out var retainerSell) && GenericHelpers.IsAddonReady(&retainerSell->AtkUnitBase))
        {
          var ui = &retainerSell->AtkUnitBase;
          var itemName = retainerSell->ItemName->NodeText.ToString();
          if (_newPrice.HasValue && _newPrice > 0)
          {
            Svc.Log.Debug($"Setting new price");
            _cachedPrices.TryAdd(itemName, _newPrice);

            retainerSell->AskingPrice->SetValue(_newPrice.Value);
            ECommons.Automation.Callback.Fire(&retainerSell->AtkUnitBase, true, 0); // confirm
            ui->Close(true);
            return true;
          }
          else
          {
            Svc.Log.Warning("SetNewPrice: No price to set");
            if (Plugin.Configuration.ShowErrorsInChat)
              Svc.Chat.PrintError($"{itemName}: No price to set, please set price manually");
            ECommons.Automation.Callback.Fire(&retainerSell->AtkUnitBase, true, 1); // cancel
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
        _skipCurrentItem = false;
      }
    }

    private void MBHandler_NewPriceReceived(object? sender, NewPriceEventArgs e)
    {
      Svc.Log.Debug($"New price received: {e.NewPrice}");
      _newPrice = e.NewPrice;
    }

    private unsafe void SkipRetainerDialog(AddonEvent type, AddonArgs args)
    {
      // fallback for when something was improperly cleaned up
      if (!_taskManager.IsBusy)
        RemoveTalkAddonListeners();
      else
      {
        if (((AtkUnitBase*)args.Addon.Address)->IsVisible)
          new AddonMaster.Talk(args.Addon).Click();
      }
    }

    private void RetainerSellPostSetup(AddonEvent type, AddonArgs args)
    {
      if (_taskManager.IsBusy)
        return;

      if (Plugin.Configuration.EnablePostPinchkey && Plugin.KeyState[Plugin.Configuration.PostPinchKey])
      {
        _taskManager.Enqueue(ClickComparePrice, $"ClickComparePricePosted");
        _taskManager.DelayNext(Plugin.Configuration.MarketBoardKeepOpenMS);
        _taskManager.Enqueue(SetNewPrice, $"SetNewPricePosted");
      }
    }
    private void RemoveTalkAddonListeners()
    {
      Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Talk", SkipRetainerDialog);
      Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "Talk", SkipRetainerDialog);
    }

    private static unsafe Vector2 GetNodePosition(AtkResNode* node)
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

    private static unsafe Vector2 GetNodeScale(AtkResNode* node)
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

    private static bool? SpeakTTS(string msg)
    {
      if (!Plugin.Configuration.DontUseTTS)
      {
        SpeechSynthesizer tts = new()
        {
            Volume = Plugin.Configuration.TTSVolume
        };
        tts.SpeakAsync(msg);
        tts.SpeakCompleted += (o, e) =>
        {
            tts.Dispose();
            Svc.Log.Verbose($"Finished message: {msg} - tts disposed");
        };
      }
      return true;
    }

    private void ClearState()
    {
      _newPrice = null;
      _cachedPrices = [];
      _skipCurrentItem = false;
    }
  }
}