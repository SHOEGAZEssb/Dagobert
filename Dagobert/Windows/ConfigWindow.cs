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

    var reopen = Plugin.Configuration.HQ;
    if (ImGui.Checkbox("Reopen Retainer", ref reopen))
    {
      Plugin.Configuration.ReopenRetainer = reopen;
      Plugin.Configuration.Save();
    }
  }
}
