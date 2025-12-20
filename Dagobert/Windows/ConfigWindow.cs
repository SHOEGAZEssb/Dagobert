using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using static ECommons.GenericHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dagobert.Windows;

public sealed class ConfigWindow : Window
{
  private static readonly string[] _virtualKeyStrings = Enum.GetNames<VirtualKey>();

  public ConfigWindow()
    : base("Dagobert Configuration")
  { }

  public override void Draw()
  {
    var hq = Plugin.Configuration.HQ;
    if (ImGui.Checkbox("Use HQ price", ref hq))
    {
      Plugin.Configuration.HQ = hq;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If checked, will use the hq price (if item is hq; will fail if there is no HQ price on the MB)");
      ImGui.EndTooltip();
    }

    ImGui.Separator();

    ImGui.BeginGroup();
    ImGui.Text("Undercut Mode:");
    ImGui.SameLine();
    var enumValues = Enum.GetNames<UndercutMode>();
    int index = Array.IndexOf(enumValues, Plugin.Configuration.UndercutMode.ToString());
    if (ImGui.Combo("##undercutModeCombo", ref index, enumValues, enumValues.Length))
    {
      var value = Enum.Parse<UndercutMode>(enumValues[index]);
      if (value == UndercutMode.Percentage && Plugin.Configuration.UndercutAmount >= 100)
        Plugin.Configuration.UndercutAmount = 1;

      Plugin.Configuration.UndercutMode = value;
      Plugin.Configuration.Save();
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Defines wether to undercut by a fixed Gil amount or use a percentage");
      ImGui.EndTooltip();
    }

    ImGui.BeginGroup();
    ImGui.Text("Undercut amount:");
    ImGui.SameLine();
    int amount = Plugin.Configuration.UndercutAmount;
    if (Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount)
    {
      if (ImGui.InputInt("##undercutAmountFixed", ref amount))
      {
        Plugin.Configuration.UndercutAmount = Math.Clamp(amount, 1, int.MaxValue);
        Plugin.Configuration.Save();
      }
    }
    else
    {
      if (ImGui.SliderInt("##undercutAmountPercentage", ref amount, 1, 99))
      {
        Plugin.Configuration.UndercutAmount = amount;
        Plugin.Configuration.Save();
      }
    }
    ImGui.SameLine();
    ImGui.Text($"{(Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount ? "Gil" : "%%")}");
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Sets the amount by which to undercut");
      ImGui.EndTooltip();
    }
    
    ImGui.BeginGroup();
    ImGui.Text("Max Undercut percentage:");
    ImGui.SameLine();
    float maxUndercut = Plugin.Configuration.MaxUndercutPercentage;
    if (ImGui.SliderFloat("##maximumUndercutAmountPercentage", ref maxUndercut, 0.1f, 99.9f, "%.1f"))
    {
      Plugin.Configuration.MaxUndercutPercentage = MathF.Round(maxUndercut, 1);
      Plugin.Configuration.Save();
    }
    ImGui.SameLine();
    ImGui.Text($"%");
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Sets the max amount of percentage allowed to be undercut");
      ImGui.EndTooltip();
    }

    var undercutSelf = Plugin.Configuration.UndercutSelf;
    if (ImGui.Checkbox("Undercut Self", ref undercutSelf))
    {
      Plugin.Configuration.UndercutSelf = undercutSelf;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If checked, your own retainer listings will be undercut");
      ImGui.EndTooltip();
    }

    ImGui.Separator();

    int currentMBDelay = Plugin.Configuration.GetMBPricesDelayMS;
    ImGui.BeginGroup();
    ImGui.Text("Market Board Price Check Delay (ms)");
    if (ImGui.SliderInt("###sliderMBDelay", ref currentMBDelay, 1, 10000))
    {
      Plugin.Configuration.GetMBPricesDelayMS = currentMBDelay;
      Plugin.Configuration.Save();
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Delay in milliseconds before opening the market board price list.\r\n" +
                       "Lower delay means faster auto pinching but may also cause market board price data to be unable to load.\r\n" +
                       "Recommended to keep between 3000 and 4000ms. Reduce at your own risk!");
      ImGui.EndTooltip();
    }

    int currentMBKeepOpenDelay = Plugin.Configuration.MarketBoardKeepOpenMS;
    ImGui.BeginGroup();
    ImGui.Text("Market Board Keep Open Time (ms)");
    if (ImGui.SliderInt("###sliderMBKeepOpen", ref currentMBKeepOpenDelay, 1, 10000))
    {
      Plugin.Configuration.MarketBoardKeepOpenMS = currentMBKeepOpenDelay;
      Plugin.Configuration.Save();
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Time in milliseconds to keep the marketboard open when fetching prices.\r\n" +
                       "Lower delay means faster auto pinching but may also cause market board price data to be unable to load.\r\n" +
                       "Recommended to keep between 1000 and 2000ms. Reduce at your own risk!");
      ImGui.EndTooltip();
    }

    bool chatErrors = Plugin.Configuration.ShowErrorsInChat;
    if (ImGui.Checkbox("Show errors in chat", ref chatErrors))
    {
      Plugin.Configuration.ShowErrorsInChat = chatErrors;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If enabled shows pinching errors in the chat.");
      ImGui.EndTooltip();
    }
    
    bool adjustmentsMessages = Plugin.Configuration.ShowPriceAdjustmentsMessages;
    if (ImGui.Checkbox("Show Price Adjustments", ref adjustmentsMessages))
    {
      Plugin.Configuration.ShowPriceAdjustmentsMessages = adjustmentsMessages;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If enabled shows detailed price adjustments messages in the chat.");
      ImGui.EndTooltip();
    }

    ImGui.SameLine(0, 40);
    
    bool retainerNames = Plugin.Configuration.ShowRetainerNames
      ;
    if (ImGui.Checkbox("Show Retainer Names", ref retainerNames))
    {
      Plugin.Configuration.ShowRetainerNames = retainerNames;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If enabled, when pinching all retainers, the name of the retainer will be printed in the chat.");
      ImGui.EndTooltip();
    }
    

    ImGui.Separator();

    ImGui.Text("Retainer Selection");
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Select which retainers should be included in auto pinch.\r\n" +
                       "Unchecked retainers will be skipped when using 'Auto Pinch' on all retainers.\r\n" +
                       "Note: Open the retainer list in-game to see and configure your retainers.");
      ImGui.EndTooltip();
    }

    // Try to fetch retainer names from the RetainerList addon if available
    unsafe
    {
      string[] retainerNameArray = null;
      bool namesUpdated = false;
      
      if (TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) && IsAddonReady(addon))
      {
        try
        {
          var retainerList = new AddonMaster.RetainerList(addon);
          retainerNameArray = retainerList.Retainers.Select(r => r.Name).ToArray();
          
          // Update stored retainer names if they changed
          var currentNames = new HashSet<string>(retainerNameArray);
          var storedNames = new HashSet<string>(Plugin.Configuration.LastKnownRetainerNames);
          
          if (!currentNames.SetEquals(storedNames))
          {
            // Names changed - update the stored list
            Plugin.Configuration.LastKnownRetainerNames = retainerNameArray.ToList();
            
            // Remove enabled status for retainers that no longer exist
            Plugin.Configuration.EnabledRetainerNames.RemoveWhere(name => !currentNames.Contains(name) && name != Configuration.ALL_DISABLED_SENTINEL);
            
            Plugin.Configuration.Save();
            namesUpdated = true;
          }
        }
        catch
        {
          // Fallback if we can't read retainer names
        }
      }

      // Use fetched names if available, otherwise use stored names
      var namesToDisplay = retainerNameArray ?? Plugin.Configuration.LastKnownRetainerNames.ToArray();

      // Only display checkboxes if we have retainer names (either fetched or stored)
      if (namesToDisplay.Length > 0)
      {
        for (int i = 0; i < namesToDisplay.Length; i++)
        {
          string retainerName = namesToDisplay[i];
          
          // Empty set = all enabled, sentinel = all disabled, non-empty = explicit whitelist
          bool allDisabled = Plugin.Configuration.EnabledRetainerNames.Contains(Configuration.ALL_DISABLED_SENTINEL);
          bool enabled = allDisabled ? false : 
                        (Plugin.Configuration.EnabledRetainerNames.Count == 0 ? true : 
                         Plugin.Configuration.EnabledRetainerNames.Contains(retainerName));
          
          string label = $"{retainerName}##retainer{i}";
          if (ImGui.Checkbox(label, ref enabled))
          {
            Plugin.Configuration.EnabledRetainerNames.Remove(Configuration.ALL_DISABLED_SENTINEL);
            
            if (enabled)
            {
              Plugin.Configuration.EnabledRetainerNames.Add(retainerName);
              // Optimize: if all retainers are enabled, clear set to use default "all enabled" mode
              if (Plugin.Configuration.EnabledRetainerNames.Count == namesToDisplay.Length)
              {
                Plugin.Configuration.EnabledRetainerNames.Clear();
              }
            }
            else
            {
              // Transition from "all enabled" (empty set) to explicit whitelist
              if (Plugin.Configuration.EnabledRetainerNames.Count == 0)
              {
                foreach (string name in namesToDisplay)
                {
                  if (name != retainerName)
                  {
                    Plugin.Configuration.EnabledRetainerNames.Add(name);
                  }
                }
              }
              else
              {
                Plugin.Configuration.EnabledRetainerNames.Remove(retainerName);
                // Use sentinel to mark "all disabled" state (empty set means "all enabled")
                if (Plugin.Configuration.EnabledRetainerNames.Count == 0)
                {
                  Plugin.Configuration.EnabledRetainerNames.Add(Configuration.ALL_DISABLED_SENTINEL);
                }
              }
            }
            Plugin.Configuration.Save();
          }
          
          // Place next checkbox on same line if it's an even index (0, 2, 4, 6, 8)
          if (i % 2 == 0 && i < namesToDisplay.Length - 1)
            ImGui.SameLine(0, 150);
        }
        
        if (retainerNameArray == null && !namesUpdated)
        {
          ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1), "(Using cached retainer list - open retainer list to refresh)");
        }
      }
      else
      {
        ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "Open retainer list in-game to configure retainer selection");
      }
    }

    ImGui.Separator();

    bool enablePostPinchKey = Plugin.Configuration.EnablePostPinchkey;
    if (ImGui.Checkbox("Enable Post Pinch Hotkey", ref enablePostPinchKey))
    {
      Plugin.Configuration.EnablePostPinchkey = enablePostPinchKey;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If enabled allows you to hold a specified key to automatically get the lowest price when posting an item to the market board.");
      ImGui.EndTooltip();
    }

    ImGui.BeginGroup();
    if (enablePostPinchKey)
    {
      ImGui.Text("Auto Post Pinch Key:");
      ImGui.SameLine();

      index = Array.IndexOf(_virtualKeyStrings, Plugin.Configuration.PostPinchKey.ToString());
      if (ImGui.Combo("##postPinchKeyCombo", ref index, _virtualKeyStrings, _virtualKeyStrings.Length))
      {
        Plugin.Configuration.PostPinchKey = Enum.Parse<VirtualKey>(_virtualKeyStrings[index]);
        Plugin.Configuration.Save();
      }
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("The key to hold to start the auto pinching process for the newly posted item.\r\n" +
                       "Be aware that the configured key still does every other hotkey action it is configured for.");
      ImGui.EndTooltip();
    }

    bool enablePinchKey = Plugin.Configuration.EnablePinchKey;
    if (ImGui.Checkbox("Enable Pinch Hotkey", ref enablePinchKey))
    {
      Plugin.Configuration.EnablePinchKey = enablePinchKey;
      Plugin.Configuration.Save();
    }
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("If enabled allows you to press a specified key to start the auto pinching process.");
      ImGui.EndTooltip();
    }

    ImGui.BeginGroup();
    if (enablePinchKey)
    {
      ImGui.Text("Auto Pinch Key:");
      ImGui.SameLine();

      string currentKey = Plugin.Configuration.PinchKey.ToString();
      index = Array.IndexOf(_virtualKeyStrings, currentKey);
      if (ImGui.Combo("##pinchKeyCombo", ref index, _virtualKeyStrings, _virtualKeyStrings.Length))
      {
        Plugin.Configuration.PinchKey = Enum.Parse<VirtualKey>(_virtualKeyStrings[index]);
        Plugin.Configuration.Save();
      }
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("The key to press to start the auto pinching process.\r\n" +
                       "Be aware that the configured key still does every other hotkey action it is configured for.");
      ImGui.EndTooltip();
    }

    if (!Plugin.Configuration.DontUseTTS)
    {
      ImGui.Separator();
      ImGui.Text("Text-To-Speech");

      ImGui.BeginGroup();
      bool ttsall = Plugin.Configuration.TTSWhenAllDone;
      if (ImGui.Checkbox("All", ref ttsall))
      {
        Plugin.Configuration.TTSWhenAllDone = ttsall;
        Plugin.Configuration.Save();
      }
      ImGui.SameLine();
      string ttsallmsg = Plugin.Configuration.TTSWhenAllDoneMsg;
      if (ImGui.InputText("##ttsallmsg", ref ttsallmsg, 256, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
      {
        Plugin.Configuration.TTSWhenAllDoneMsg = ttsallmsg;
        Plugin.Configuration.Save();
      }
      ImGui.EndGroup();
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        ImGui.SetTooltip("If checked, will use Windows TTS to say the configured phrase once Auto Pinch has processed all retainers");
        ImGui.EndTooltip();
      }
      
      ImGui.BeginGroup();
      bool ttseach = Plugin.Configuration.TTSWhenEachDone;
      if (ImGui.Checkbox("Each", ref ttseach))
      {
        Plugin.Configuration.TTSWhenEachDone = ttseach;
        Plugin.Configuration.Save();
      }
      ImGui.SameLine();
      string ttseachmsg = Plugin.Configuration.TTSWhenEachDoneMsg;
      if (ImGui.InputText("##ttseachmsg", ref ttseachmsg, 256, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
      {
        Plugin.Configuration.TTSWhenEachDoneMsg = ttseachmsg;
        Plugin.Configuration.Save();
      }
      ImGui.EndGroup();
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        ImGui.SetTooltip("If checked, will use Windows TTS to say the configured phrase once Auto Pinch has processed the current retainer's listings");
        ImGui.EndTooltip();
      }

      ImGui.BeginGroup();
      ImGui.Text("TTS Volume:");
      ImGui.SameLine();
      int volume = Plugin.Configuration.TTSVolume;
      if (ImGui.SliderInt("##ttsVolumeAmount", ref volume, 1, 99))
      {
        Plugin.Configuration.TTSVolume = volume;
        Plugin.Configuration.Save();
      }
      ImGui.SameLine();
      ImGui.Text("%");
      ImGui.EndGroup();
      if (ImGui.IsItemHovered())
      {
        ImGui.BeginTooltip();
        ImGui.SetTooltip("Sets the volume of the Text-to-speech message");
        ImGui.EndTooltip();
      }
    }
  }
}