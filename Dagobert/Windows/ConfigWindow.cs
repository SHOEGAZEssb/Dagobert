using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;

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