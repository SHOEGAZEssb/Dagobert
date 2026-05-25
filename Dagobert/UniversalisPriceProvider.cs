using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dagobert;

internal sealed class UniversalisPriceProvider : IDisposable
{
  private readonly Lumina.Excel.ExcelSheet<Item> _items;
  private readonly UniversalisClient _client;

  public UniversalisPriceProvider()
  {
    _items = Svc.Data.GetExcelSheet<Item>();
    _client = new UniversalisClient();
  }

  public bool CanResolveItem(string itemName, string rawItemName) => TryGetItem(itemName, rawItemName, out _, out _);

  public async Task<int> GetNewPrice(string itemName, string rawItemName, CancellationToken cancellationToken)
  {
    if (!TryGetItem(itemName, rawItemName, out var itemId, out var hqOnly))
    {
      Svc.Log.Warning($"Could not resolve item id for Universalis price check: {NormalizeItemName(itemName)}");
      return -1;
    }

    var dataCenterName = Svc.Objects.LocalPlayer?.CurrentWorld.ValueNullable?.DataCenter.ValueNullable?.Name.ToString();
    if (string.IsNullOrWhiteSpace(dataCenterName))
    {
      Svc.Log.Warning("Could not resolve current data center for Universalis price check");
      return -1;
    }

    var marketData = await _client.GetMarketData(itemId, dataCenterName, hqOnly, cancellationToken).ConfigureAwait(false);
    if (!marketData.HasData || marketData.Listings.Count == 0)
      return -1;

    var listing = marketData.Listings
      .Where(listing => listing.PricePerUnit > 0 && (!hqOnly || listing.Hq))
      .OrderBy(listing => listing.PricePerUnit)
      .FirstOrDefault();

    if (listing == null)
      return -1;

    var ownRetainer = ulong.TryParse(listing.RetainerId, out var retainerId)
                      && Plugin.Configuration.SeenRetainers.Contains(retainerId);
    Svc.Log.Debug($"Universalis lowest data center price: {listing.PricePerUnit} on {listing.WorldName ?? dataCenterName}");
    return CalculateNewPrice(listing.PricePerUnit, ownRetainer);
  }

  public void Dispose()
  {
    _client.Dispose();
  }

  private bool TryGetItem(string itemName, string rawItemName, out uint itemId, out bool hqOnly)
  {
    var itemHq = itemName.Contains('\uE03C') || rawItemName.Contains('\uE03C');
    var normalizedItemName = NormalizeItemName(itemName);

    itemId = ResolveItemId(normalizedItemName, itemName);
    if (itemId == 0)
      itemId = ResolveItemId(normalizedItemName, rawItemName);

    hqOnly = Plugin.Configuration.HQ && itemHq;
    return itemId != 0;
  }

  private uint ResolveItemId(string normalizedItemName, string rawItemName)
  {
    var exactMatch = _items
      .Where(item => item.Name.GetText().Equals(normalizedItemName, StringComparison.OrdinalIgnoreCase))
      .Select(item => item.RowId)
      .FirstOrDefault();

    if (exactMatch != 0)
      return exactMatch;

    return _items
      .Select(item => new
      {
        item.RowId,
        Name = item.Name.GetText(),
      })
      .Where(item => item.Name.Length > 0 && rawItemName.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
      .OrderByDescending(item => item.Name.Length)
      .Select(item => item.RowId)
      .FirstOrDefault();
  }

  private static int CalculateNewPrice(long pricePerUnit, bool ownRetainer)
  {
    var price = (int)Math.Min(pricePerUnit, int.MaxValue);

    if (!Plugin.Configuration.UndercutSelf && ownRetainer)
      return price;

    if (Plugin.Configuration.UndercutMode == UndercutMode.FixedAmount)
      return Math.Max(price - Plugin.Configuration.UndercutAmount, 1);

    return (int)Math.Max((100L - Plugin.Configuration.UndercutAmount) * price / 100L, 1);
  }

  private static string NormalizeItemName(string itemName)
  {
    var normalizedItemName = itemName.Replace("\uE03C", string.Empty).Trim();

    try
    {
      var text = SeString.Parse(Encoding.UTF8.GetBytes(normalizedItemName)).GetText().Trim();
      if (!string.IsNullOrEmpty(text))
        normalizedItemName = text;
    }
    catch
    {
      // If this is already plain text, or malformed SeString text, fall back to the raw visible substring matching.
    }

    return normalizedItemName;
  }
}
