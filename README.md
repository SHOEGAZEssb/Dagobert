# Ascended Dagobert

This is a personal fork of [SHOEGAZEssb/Dagobert](https://github.com/SHOEGAZEssb/Dagobert). It tracks upstream and adds bait-listing protection to the undercut logic. If you don't need that behavior, install upstream Dagobert instead.

## What this fork adds

**Bait Listing Protection.** Upstream Dagobert cuts to the absolute lowest listing on the market board, which lets a single 1-unit decoy at 5 gil drag every retainer's price to the floor while real stacks sit at 1.2k. This fork filters listings for credibility before picking a target, then undercuts the lowest *credible* listing instead.

Three filters apply, in order:

1. **Minimum stack size.** Listings below the configured quantity threshold are ignored (set higher for items normally sold in stacks).
2. **Stock-weighted price floor.** A weighted median is computed across the cheapest N units of supply. Listings below `FloorPercent` of that median are rejected as bait. A 99-unit stack contributes 99 votes; a 1-unit decoy contributes 1.
3. **Price-gap detector.** If the cheapest credible listing sits below `GapPercent` of the second-cheapest, it gets skipped and the second is targeted instead.

If no listing passes the filters, the bot holds off rather than committing to bait. All four thresholds are configurable in the "Bait Listing Protection" section of the config window, and the feature can be toggled off entirely to fall back to the legacy "cut to absolute lowest" behavior.

Defaults: 30% floor, 10-unit sample, 50% gap, min-quantity 1.

## Upstream features

Inherited unchanged from upstream:

- Automatic market board undercutting by fixed gil or percentage
- Configurable per-item behavior
- Text-to-speech notifications on completed pinches

## Installing

This plugin is distributed through the [ascended-plugins](https://github.com/jkleinne/ascended-plugins) Dalamud repository, which aggregates all `ascended-*` plugin forks under a single URL. Add it once and any future plugin in that namespace becomes available without extra repository entries.

- Open `/xlsettings` in chat and switch to the Experimental tab
- Scroll past DevPlugins to the Custom Plugin Repositories section
- Paste this URL into a free text input:

```
https://raw.githubusercontent.com/jkleinne/ascended-plugins/main/pluginmaster.json
```

- Click `+`, tick the new entry's checkbox, and save
- Reopen Dalamud's plugin installer; "Ascended Dagobert" appears under Available Plugins

**Coexistence with upstream Dagobert:** this fork uses a distinct `InternalName` (`ascended-dagobert`), so Dalamud treats it as a separate plugin. It does, however, register the same `/dagobert` chat command as upstream, so the two cannot run simultaneously without command-registration conflicts. Uninstall upstream Dagobert before installing this fork.

If you'd rather use the official upstream distribution:

```
https://raw.githubusercontent.com/SHOEGAZEssb/DalamudPluginRepo/master/pluginmaster.json
```

## Contributing

Fork-specific behavior (currently just Bait Listing Protection) is fair game for issues and PRs against this repository. General Dagobert changes should be contributed upstream at [SHOEGAZEssb/Dagobert](https://github.com/SHOEGAZEssb/Dagobert); upstream commits flow into this fork on the next sync.
