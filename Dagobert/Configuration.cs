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
  
  public float MaxUndercutPercentage { get; set; } = 100.0f;

  public bool UndercutSelf { get; set; } = false;
  
  public bool ShowPriceAdjustmentsMessages { get; set; } = true;
  
  public bool ShowRetainerNames { get; set; } = true;

  public bool TTSWhenAllDone { get; set; } = false;

  public string TTSWhenAllDoneMsg { get; set; } = "Finished auto pinching all retainers";

  public bool TTSWhenEachDone { get; set; } = false;

  public string TTSWhenEachDoneMsg { get; set; } = "Auto Pinch done";

  public int TTSVolume { get; set; } = 20;

  public bool DontUseTTS { get; set; } = false;

  /// <summary>
  /// Array of 10 booleans indicating which retainers are enabled for auto pinch.
  /// Index corresponds to retainer position (0-9).
  /// true = enabled, false = excluded
  /// </summary>
  public bool[] EnabledRetainers { get; set; } = new bool[10] { true, true, true, true, true, true, true, true, true, true };

  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}