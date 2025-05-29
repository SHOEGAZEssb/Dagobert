using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;

namespace Dagobert;

public enum UndercutMode
{
  FixedAmount,
  Percentage
}

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
  public int Version { get; set; } = 0;

  public bool HQ { get; set; } = true;

  public int GetMBPricesDelayMS { get; set; } = 3000;

  public int MarketBoardKeepOpenMS { get; set; } = 1000;

  public bool ShowErrorsInChat { get; set; } = true;

  public bool EnablePinchKey { get; set; } = false;

  public VirtualKey PinchKey { get; set; } = VirtualKey.Q;

  public bool EnablePostPinchkey { get; set; } = true;

  public VirtualKey PostPinchKey { get; set; } = VirtualKey.SHIFT;

  public UndercutMode UndercutMode { get; set; } = UndercutMode.FixedAmount;

  public int UndercutAmount { get; set; } = 1;

  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}