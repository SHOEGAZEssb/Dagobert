using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
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

  public bool CanResolveItem(string itemName) => TryGetItem(itemName, out _, out _);

  public async Task<int> GetNewPrice(string itemName, CancellationToken cancellationToken)
  {
    if (!TryGetItem(itemName, out var itemId, out var hqOnly))
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

  private bool TryGetItem(string itemName, out uint itemId, out bool hqOnly)
  {
    var itemHq = itemName.Contains('\uE03C');
    var normalizedItemName = NormalizeItemName(itemName);

    itemId = _items
      .Where(item => item.Name.ToString().Equals(normalizedItemName, StringComparison.Ordinal))
      .Select(item => item.RowId)
      .FirstOrDefault();

    hqOnly = Plugin.Configuration.HQ && itemHq;
    return itemId != 0;
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
    return itemName.Replace("\uE03C", string.Empty).Trim();
  }
}
