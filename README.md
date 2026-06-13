# Dagobert

FFXIV Dalamud plugin for automatically adjusting retainer Market Board prices.

## Installation

Add this repository to your Dalamud custom plugin repositories:

```text
https://raw.githubusercontent.com/SHOEGAZEssb/DalamudPluginRepo/master/pluginmaster.json
```

Open the configuration window with `/dagobert`.

## Configuration

The `/dagobert` configuration window has two tabs:

- `General` contains the normal pricing, timing, retainer, hotkey, chat, and TTS settings.
- `Min/Max Prices` contains per-item minimum and maximum prices.

### Pricing

| Option | Default | Description |
| --- | --- | --- |
| Use HQ price | On | Uses HQ listings for HQ items. If no HQ listing is available, Dagobert may not find a price. |
| Undercut Mode | FixedAmount | Choose between subtracting a fixed gil amount or a percentage from the lowest price. |
| Undercut amount | 1 gil | How much to undercut by. In Percentage mode this is 1-99%. |
| Max Undercut percentage | 100% | Safety limit. Price changes that would cut more than this percentage are skipped. |
| Undercut Self | Off | When off, Dagobert will not undercut listings from your own retainers. |
| Use Universalis data center prices | Off | Uses the cheapest listing on your current data center from Universalis instead of only the in-game Market Board result. |
| Default amount | 0 gil | Fallback price when no price can be found. `0` disables the fallback. |
| Show inventory context menu entry | On | Adds the right-click inventory entry used to add or configure per-item min/max prices. |

### Per-Item Min/Max Prices

Use the `Min/Max Prices` tab to set item-specific price limits. `0` means no limit.

| Option | Description |
| --- | --- |
| Min | Lowest price Dagobert may set for that item. |
| Max | Highest price Dagobert may set for that item. |
| Remove | Deletes the item-specific limits. |

Right-click an item in your inventory and choose `Add Dagobert price limits` to add it to the table. If the item is already configured, the same menu entry opens the configuration window as `Configure Dagobert price limits`.

Disable `Show inventory context menu entry` in the `General` tab to hide this right-click entry.

Per-item limits are applied after Dagobert finds a candidate price, including prices from Universalis, and before the price is written to the retainer listing.

### Timing

| Option | Default | Description |
| --- | --- | --- |
| Market Board Price Check Delay | 3000 ms | Delay before opening the Market Board price list. Lower values are faster but less reliable. |
| Market Board Keep Open Time | 1000 ms | How long the Market Board window stays open while fetching prices. Lower values are faster but less reliable. |

Recommended timing values are around `3000-4000 ms` for the price check delay and `1000-2000 ms` for the keep-open time.

### Retainers and Chat

| Option | Default | Description |
| --- | --- | --- |
| Retainer Selection | All enabled | Choose which retainers are included when using Auto Pinch from the retainer list. Open the retainer list in-game to populate or refresh this list. |
| Show errors in chat | On | Prints pinching errors and skipped items to chat. |
| Show Price Adjustments | On | Prints detailed price changes to chat. |
| Show Retainer Names | On | Prints each retainer name while pinching all retainers. |
| Clear retainer Cache | - | Clears the stored list of your own retainers used for self-undercut detection. |

### Hotkeys

| Option | Default | Description |
| --- | --- | --- |
| Enable Post Pinch Hotkey | On | Lets you hold the configured key while posting a new item to automatically get the lowest price. |
| Auto Post Pinch Key | Shift | Key used for post pinch. |
| Enable Pinch Hotkey | Off | Lets you press the configured key to start Auto Pinch from the retainer list or retainer sell list. |
| Auto Pinch Key | Q | Key used for Auto Pinch. |

Configured hotkeys still perform their normal in-game actions.

### Text-to-Speech

The Text-to-Speech options are shown when Windows TTS is available.

| Option | Default | Description |
| --- | --- | --- |
| All | Off | Speaks a message after Auto Pinch has processed all retainers. |
| Each | Off | Speaks a message after Auto Pinch has processed the current retainer. |
| TTS Volume | 20% | Volume for spoken messages. |

## Usage

Dagobert adds an `Auto Pinch` button to the retainer list and retainer sell list. While Auto Pinch is running, avoid interacting with the game until it finishes or you press `Cancel`.
