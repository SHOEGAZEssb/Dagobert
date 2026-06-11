using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dagobert;

public enum UndercutMode
{
  FixedAmount,
  Percentage
}

[Serializable]
public sealed class ItemPriceLimit
{
  public uint ItemId { get; set; }

  public int MinPrice { get; set; } = 0;

  public int MaxPrice { get; set; } = 0;

  public int Apply(int price)
  {
    var minPrice = Math.Max(MinPrice, 0);
    var maxPrice = Math.Max(MaxPrice, 0);

    if (minPrice > 0 && price < minPrice)
      price = minPrice;

    if (maxPrice > 0)
    {
      if (minPrice > 0 && maxPrice < minPrice)
        maxPrice = minPrice;

      if (price > maxPrice)
        price = maxPrice;
    }

    return price;
  }
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

  public int DefaultAmount { get; set; } = 0;

  public int UndercutAmount { get; set; } = 1;

  public float MaxUndercutPercentage { get; set; } = 100.0f;

  public bool UndercutSelf { get; set; } = false;

  public bool UseUniversalisDataCenterPrices { get; set; } = false;

  public bool ShowPriceAdjustmentsMessages { get; set; } = true;

  public bool ShowRetainerNames { get; set; } = true;

  public bool TTSWhenAllDone { get; set; } = false;

  public string TTSWhenAllDoneMsg { get; set; } = "Finished auto pinching all retainers";

  public bool TTSWhenEachDone { get; set; } = false;

  public string TTSWhenEachDoneMsg { get; set; } = "Auto Pinch done";

  public int TTSVolume { get; set; } = 20;

  public bool DontUseTTS { get; set; } = false;

  public List<ulong> SeenRetainers { get; set; } = [];

  public List<ItemPriceLimit> ItemPriceLimits { get; set; } = [];

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

  public ItemPriceLimit? GetItemPriceLimit(uint itemId)
  {
    return ItemPriceLimits.FirstOrDefault(limit => limit.ItemId == itemId);
  }

  public ItemPriceLimit GetOrAddItemPriceLimit(uint itemId)
  {
    var limit = GetItemPriceLimit(itemId);
    if (limit != null)
      return limit;

    limit = new ItemPriceLimit { ItemId = itemId };
    ItemPriceLimits.Add(limit);
    return limit;
  }

  public void Save()
  {
    Plugin.PluginInterface.SavePluginConfig(this);
  }
}
