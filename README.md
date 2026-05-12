# Ascended Dagobert, personal fork of Dagobert

This is a personal fork of [SHOEGAZEssb/Dagobert](https://github.com/SHOEGAZEssb/Dagobert). It tracks upstream and currently carries no fork-specific behavior. If you don't have a specific reason to use this fork, install upstream Dagobert instead.

The fork exists so that any future modifications can be packaged alongside other `ascended-*` plugin forks under a single Dalamud repository.

## What this fork adds

Nothing yet. Upstream behavior unchanged.

## Upstream features

Inherited unchanged from upstream:

- Automatic market board undercutting by 1 gil
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

There are no fork-specific features to contribute against yet. General Dagobert changes should be contributed upstream at [SHOEGAZEssb/Dagobert](https://github.com/SHOEGAZEssb/Dagobert); upstream commits flow into this fork on the next sync.
