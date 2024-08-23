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

    ImGui.BeginGroup();      
    ImGui.Text("Shift Key Behaviour:");
    var behaviours = new string[] { "Reopen retainer", "Don't reopen retainer" };
    var currentBehaviour = (int)Plugin.Configuration.ShiftBehaviour;
    if (ImGui.Combo("###shiftBehaviour", ref currentBehaviour, behaviours, behaviours.Length))
    {
      Plugin.Configuration.ShiftBehaviour = (ShiftBehaviour)currentBehaviour;
      Plugin.Configuration.Save();
    }
    ImGui.EndGroup();
    if (ImGui.IsItemHovered())
    {
      ImGui.BeginTooltip();
      ImGui.SetTooltip("Defines what to do when the shift key is held when pressing the auto pinch button.\r\n" +
                       "When adding new items to the market board you will need to reopen your retainer for them to be able to be pinched");
      ImGui.EndTooltip();
    }
  }
}
