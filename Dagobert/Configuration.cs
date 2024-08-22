using Dalamud.Configuration;
using System;

namespace Dagobert;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public bool HQ { get; set; } = true;

  public bool ReopenRetainer { get; set; } = true;

  // the below exist just to make saving less cumbersome
  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}
