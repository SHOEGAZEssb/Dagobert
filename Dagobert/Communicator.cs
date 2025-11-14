using System;
using System.Linq;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dagobert;

public static class Communicator
{
    private static readonly ExcelSheet<Item> ItemSheet = Svc.Data.GetExcelSheet<Item>();
    
    public static void PrintPriceUpdate(string itemName, int? oldPrice, int? newPrice, float cutPercentage)
    {
        
        if(oldPrice == null || newPrice == null) return;
        if (oldPrice.Value == newPrice.Value) return;
        
        var dec = oldPrice.Value > newPrice.Value ? "cut" : "increase";
        var itemPayload = RawItemNameToItemPayload(itemName);
        
        if (itemPayload != null)
        {
            var seString = new SeStringBuilder()
                .AddItemLink(itemPayload.ItemId, itemPayload.IsHQ)
                .AddText($": Pinching from {oldPrice.Value:N0} to {newPrice.Value:N0} gil, a {dec} of {MathF.Abs(MathF.Round(cutPercentage, 2))}%")
                .Build();
    
            Svc.Chat.Print(seString);
        }
        else
        {
            Svc.Chat.Print($"{itemName}: Pinching from {oldPrice.Value:N0} to {newPrice.Value:N0}, a {dec} of {MathF.Abs(MathF.Round(cutPercentage, 2))}%");
        }
    }

    private static ItemPayload? RawItemNameToItemPayload(string itemName)
    {
        // Parse as SeString
        var seString = SeString.Parse(Encoding.UTF8.GetBytes(itemName));

        // Find all text payloads
        var textPayloads = seString.Payloads
            .OfType<TextPayload>()
            .ToList();
        
        var cleanedName = "";
        var isHq = false;
        
        // The actual item name is in the second TextPayload 
        if (textPayloads.Count >= 2)
        {
            var itemNamePayload = textPayloads[1].Text;
            
            // Remove the prefix: & (U+0026) followed by ETX (U+0003)
            if (itemNamePayload != null &&
                itemNamePayload.Length >= 2 && 
                itemNamePayload[0] == '\u0026' && 
                itemNamePayload[1] == '\u0003')
            {
                cleanedName = itemNamePayload.Substring(2);
            }
            else
            {
                cleanedName = itemNamePayload;
            }
            
            // Check for HQ symbol at the end: space + U+E03C
            if (cleanedName != null &&
                cleanedName.Length >= 2 && 
                cleanedName[^1] == '\uE03C')
            {
                isHq = true;
                // Remove the HQ symbol and the space before it
                cleanedName = cleanedName.Substring(0, cleanedName.Length - 2).TrimEnd();
            }
            else
            {
                cleanedName = cleanedName?.TrimEnd();
            }
        }
        else if (textPayloads.Count == 1)
        {
            cleanedName = textPayloads[0].Text!.Trim();
        }
        
        // Search for the item
        var item = ItemSheet.FirstOrDefault(i => 
            i.Name.ToString().Equals(cleanedName, StringComparison.OrdinalIgnoreCase));
        
        if (item.RowId > 0)
        {
            var itemPayloadResult = new ItemPayload(item.RowId, isHq);
            return itemPayloadResult;
        }
        
        return null;
    }
    
    public static void PrintAboveMaxCutError(string itemName)
    {
        if (Plugin.Configuration.ShowErrorsInChat)
        {
            var itemPayload = RawItemNameToItemPayload(itemName);
            
            if (itemPayload != null)
            {
                var seString = new SeStringBuilder()
                    .AddItemLink(itemPayload.ItemId, itemPayload.IsHQ)
                    .AddText($": Item ignored because it would cut the price by more than {Plugin.Configuration.MaxUndercutPercentage}%")
                    .Build();
    
                Svc.Chat.PrintError(seString);
            }
            else
            {
                Svc.Chat.PrintError($"{itemName}: Item ignored because it would cut the price by more than {Plugin.Configuration.MaxUndercutPercentage}%");
            }
        }
    }

    public static void PrintRetainerName(string name)
    {
        var seString = new SeStringBuilder()
            .AddText("Now Pinching items of retainer: ")
            .AddUiForeground(name, 561)
            .Build();
        Svc.Chat.Print(seString);
    }

    public static void PrintNoPriceToSetError(string itemName)
    {
        if (!Plugin.Configuration.ShowErrorsInChat) return;
        
        var itemPayload = RawItemNameToItemPayload(itemName);
            
        if (itemPayload != null)
        {
            var seString = new SeStringBuilder()
                .AddItemLink(itemPayload.ItemId, itemPayload.IsHQ)
                .AddText($": No price to set, please set price manually")
                .Build();
    
            Svc.Chat.PrintError(seString);
        }
        else
        {
            Svc.Chat.PrintError($"{itemName}: No price to set, please set price manually");
        }
    }
}