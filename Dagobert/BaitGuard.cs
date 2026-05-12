using Dalamud.Game.Network.Structures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dagobert;

/// <summary>
/// Picks a credible competing listing to undercut, rather than blindly taking the
/// lowest price. Defends against bait listings (e.g. a single-unit listing priced
/// far below the real market to trick a bot into undercutting to near-zero).
/// </summary>
/// <remarks>
/// Pure function — all decisions derive from the listings passed in and the
/// supplied options. No I/O, no global state.
/// </remarks>
internal static class BaitGuard
{
  public readonly record struct Options(
    bool Enabled,
    float FloorPercent,
    int SampleUnits,
    float GapPercent,
    int MinQuantity);

  /// <summary>
  /// Returns the index in <paramref name="listings"/> that should be undercut,
  /// or <c>null</c> if no listing among <paramref name="candidateIndices"/> looks
  /// credible. A null return means the caller should wait for more listings or
  /// skip the item — never undercut against unfiltered bait.
  /// </summary>
  /// <param name="listings">Full listing set (caller does not need to sort).</param>
  /// <param name="candidateIndices">
  /// Indices into <paramref name="listings"/> the caller considers eligible
  /// (HQ filter, own-retainer filter, etc. already applied).
  /// </param>
  public static int? SelectTargetIndex(
    IReadOnlyList<IMarketBoardItemListing> listings,
    IReadOnlyList<int> candidateIndices,
    Options opts)
  {
    if (candidateIndices.Count == 0)
      return null;

    if (!opts.Enabled)
      return MinByPrice(listings, candidateIndices);

    // Sort candidates cheapest-first; bait analysis only makes sense on a sorted set.
    var sorted = candidateIndices
      .OrderBy(i => listings[i].PricePerUnit)
      .ToList();

    if (sorted.Count == 1)
      return sorted[0];

    // Filter 1: minimum stack-size threshold. Items normally sold in stacks
    // (materia, gathered mats, food) frequently see 1-unit bait listings; raising
    // this knob makes the bot ignore them outright.
    var minQty = (uint)Math.Max(1, opts.MinQuantity);
    var passQty = sorted
      .Where(i => listings[i].ItemQuantity >= minQty)
      .ToList();
    if (passQty.Count == 0)
      return null;

    // Filter 2: stock-weighted price floor. Use the cheapest N units of supply
    // to anchor the market, then reject anything priced below FloorPercent of
    // that anchor. A 99-unit stack contributes 99 votes; a 1-unit decoy contributes 1.
    uint floor = ComputeStockWeightedFloor(listings, passQty, opts.SampleUnits, opts.FloorPercent);
    var passFloor = passQty
      .Where(i => listings[i].PricePerUnit >= floor)
      .ToList();
    if (passFloor.Count == 0)
      return null;

    // Filter 3: price-gap detector. Catches bait that survived the floor (e.g. a
    // medium-quantity listing priced 50%+ below the next-cheapest credible one).
    if (passFloor.Count >= 2)
    {
      var firstPrice = listings[passFloor[0]].PricePerUnit;
      var secondPrice = listings[passFloor[1]].PricePerUnit;
      if ((float)firstPrice * 100f < opts.GapPercent * secondPrice)
        return passFloor[1];
    }

    return passFloor[0];
  }

  private static int MinByPrice(
    IReadOnlyList<IMarketBoardItemListing> listings,
    IReadOnlyList<int> candidates)
  {
    int best = candidates[0];
    for (int k = 1; k < candidates.Count; k++)
      if (listings[candidates[k]].PricePerUnit < listings[best].PricePerUnit)
        best = candidates[k];
    return best;
  }

  /// <summary>
  /// Walks cheapest-first until <paramref name="sampleUnits"/> of supply is
  /// collected, then returns <paramref name="floorPercent"/>% of the weighted-median
  /// unit price across those units.
  /// </summary>
  private static uint ComputeStockWeightedFloor(
    IReadOnlyList<IMarketBoardItemListing> listings,
    IReadOnlyList<int> sortedAsc,
    int sampleUnits,
    float floorPercent)
  {
    var samples = new List<(uint price, long qty)>();
    long collected = 0;
    long cap = Math.Max(1, sampleUnits);

    foreach (var idx in sortedAsc)
    {
      var l = listings[idx];
      long take = Math.Min((long)l.ItemQuantity, cap - collected);
      if (take <= 0)
        break;

      samples.Add((l.PricePerUnit, take));
      collected += take;
      if (collected >= cap)
        break;
    }

    if (collected == 0)
      return 0;

    // Weighted median: the unit at position collected/2 in the price-sorted supply.
    long target = collected / 2;
    long running = 0;
    uint median = samples[^1].price;
    foreach (var (price, qty) in samples)
    {
      running += qty;
      if (running > target)
      {
        median = price;
        break;
      }
    }

    return (uint)(median * floorPercent / 100f);
  }
}
