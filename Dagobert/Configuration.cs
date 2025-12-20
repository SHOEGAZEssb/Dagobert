using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;
using System.Collections.Generic;

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
  /// Set of retainer names that are enabled for auto pinch.
  /// If empty or null, all retainers are enabled by default.
  /// If contains ALL_DISABLED_SENTINEL, all retainers are disabled.
  /// </summary>
  public const string ALL_DISABLED_SENTINEL = "__ALL_DISABLED__";
  
  public HashSet<string> EnabledRetainerNames { get; set; } = [];

  /// <summary>
  /// List of retainer names that were last fetched from the game.
  /// Used to display retainer selection even when the retainer list is not open.
  /// </summary>
  public List<string> LastKnownRetainerNames { get; set; } = [];

  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}