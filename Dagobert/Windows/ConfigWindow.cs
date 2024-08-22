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
    // can't ref a property, so use a local copy
    var hq = Plugin.Configuration.HQ;
    if (ImGui.Checkbox("Use HQ price", ref hq))
    {
      Plugin.Configuration.HQ = hq;
      Plugin.Configuration.Save();
    }
  }
}
