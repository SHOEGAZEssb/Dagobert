using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dagobert.Windows;
using ECommons;

namespace Dagobert;

public sealed class Plugin : IDalamudPlugin
{
  [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
  [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
  [PluginService] public static IMarketBoard MarketBoard { get; private set; } = null!;
  [PluginService] public static IKeyState KeyState { get; private set; } = null!;
  [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
  [PluginService] public static IChatGui ChatGui { get; private set; } = null!;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
  public static Configuration Configuration { get; private set; } // will never be null
  public static DalamudLinkPayload ConfigLinkPayload { get; private set; } = null!;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

  private readonly AutoPinch _autoPinch;

  public readonly WindowSystem WindowSystem = new("Dagobert");
  private ConfigWindow ConfigWindow { get; init; }

  public Plugin()
  {
    Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    ConfigWindow = new ConfigWindow();
    WindowSystem.AddWindow(ConfigWindow);

    CommandManager.AddHandler("/dagobert", new CommandInfo(OnDagobertCommand)
    {
      HelpMessage = "Opens the Dagobert configuration window"
    });

    // Register chat link handler for clickable config link
    ConfigLinkPayload = ChatGui.AddChatLinkHandler(0, (id, _) => ToggleConfigUI());

    PluginInterface.UiBuilder.Draw += DrawUI;
    PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

    ECommonsMain.Init(PluginInterface, this);
    _autoPinch = new AutoPinch();
    WindowSystem.AddWindow(_autoPinch);
  }

  public void Dispose()
  {
    WindowSystem.RemoveAllWindows();
    _autoPinch.Dispose();
    CommandManager.RemoveHandler("/dagobert");
    ECommonsMain.Dispose();
  }

  private void OnDagobertCommand(string command, string args)
  {
    // in response to the slash command, just toggle the display status of our main ui
    ToggleConfigUI();
  }

  private void DrawUI()
  {
    WindowSystem.Draw();
  }

  public void ToggleConfigUI() => ConfigWindow.Toggle();
}