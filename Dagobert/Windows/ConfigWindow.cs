using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dagobert.Windows;

public class ConfigWindow : Window, IDisposable
{
  public ConfigWindow()
    : base("Dagobert Configuration")
  { }

  public void Dispose() { }

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
      ImGui.SetTooltip("If checked, will use the hq price (if item is hq), otherwise will use cheapest price, wether it's hq or not");
      ImGui.EndTooltip();
    }

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
                       "Recommended to keep between 2000 and 3000");
      ImGui.EndTooltip();
    }

  }
}
