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
        if(!Plugin.Configuration.ShowPriceAdjustmentsMessages)
            return;
        
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
        
        if (textPayloads.Count == 0)
            return null;
        
        var cleanedName = "";
        var isHq = false;
        
        if (textPayloads.Count == 1)
        {
            // Single text payload - just trim it
            cleanedName = textPayloads[0].Text?.Trim();
        }
        else if (textPayloads.Count >= 2)
        {
            // Skip the first payload (it's always just "%" with ETX)
            // Concatenate payloads starting from index 1
            var nameParts = new StringBuilder();
            
            for (int i = 1; i < textPayloads.Count; i++)
            {
                var text = textPayloads[i].Text;
                
                // First payload after the initial marker has a prefix: ANY_CHAR + ETX (U+0003)
                if (i == 1 && text?.Length >= 2 && text[1] == '\u0003')
                {
                    text = text.Substring(2);
                }
                
                nameParts.Append(text);
            }
            
            cleanedName = nameParts.ToString();
            
            // Check and clean HQ symbol at the very end
            if (cleanedName.Length >= 1 && cleanedName[^1] == '\uE03C')
            {
                isHq = true;
                cleanedName = cleanedName.Substring(0, cleanedName.Length - 1).TrimEnd();
            }
            else
            {
                cleanedName = cleanedName.TrimEnd();
            }
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
        if (!Plugin.Configuration.ShowErrorsInChat) return;
        
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

    public static void PrintRetainerName(string name)
    {
        if (!Plugin.Configuration.ShowRetainerNames)
            return;
        
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