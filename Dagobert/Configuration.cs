using Dalamud.Configuration;
using System;

namespace Dagobert;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public bool HQ { get; set; } = true;

  public int GetAddonMaxTimeoutMS { get; set; } = 5000;

  public int GetMBPricesDelayMS { get; set; } = 3000;

  public bool ShowErrorsInChat { get; set; } = true;

  // the below exist just to make saving less cumbersome
  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}
