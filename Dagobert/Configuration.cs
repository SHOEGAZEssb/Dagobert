using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;

namespace Dagobert;

[Serializable]
public class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public bool HQ { get; set; } = true;

  public int GetMBPricesDelayMS { get; set; } = 3000;

  public bool ShowErrorsInChat { get; set; } = true;

  public bool EnablePinchKey { get; set; } = false;

  public VirtualKey PinchKey { get; set; } = VirtualKey.Q;

  // the below exist just to make saving less cumbersome
  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}
