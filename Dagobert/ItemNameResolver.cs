using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Text;

namespace Dagobert;

internal static class ItemNameResolver
{
  private static readonly Lazy<ExcelSheet<Item>> Items = new(() => Svc.Data.GetExcelSheet<Item>());

  public static bool TryGetItemId(string itemName, string rawItemName, out uint itemId)
  {
    var normalizedItemName = NormalizeItemName(itemName);

    itemId = ResolveItemId(normalizedItemName, itemName);
    if (itemId == 0)
      itemId = ResolveItemId(normalizedItemName, rawItemName);

    return itemId != 0;
  }

  public static string GetItemName(uint itemId)
  {
    if (Items.Value.TryGetRow(itemId, out var item))
      return item.Name.GetText();

    return $"Unknown item ({itemId})";
  }

  private static uint ResolveItemId(string normalizedItemName, string rawItemName)
  {
    var exactMatch = Items.Value
      .Where(item => item.Name.GetText().Equals(normalizedItemName, StringComparison.OrdinalIgnoreCase))
      .Select(item => item.RowId)
      .FirstOrDefault();

    if (exactMatch != 0)
      return exactMatch;

    return Items.Value
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
      // Plain text and malformed SeString both fall back to visible substring matching.
    }

    return normalizedItemName;
  }
}
