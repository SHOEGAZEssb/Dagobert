using Dalamud.Configuration;
using System;

namespace Dagobert;

public enum ShiftBehaviour
{
  ReopenRetainer = 0,
  DontReopenRetainer = 1
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public bool HQ { get; set; } = true;

  public ShiftBehaviour ShiftBehaviour { get; set; } = ShiftBehaviour.DontReopenRetainer;

  // the below exist just to make saving less cumbersome
  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}
