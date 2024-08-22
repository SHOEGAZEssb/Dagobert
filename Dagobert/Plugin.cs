using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
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

  public static Configuration Configuration { get; private set; }

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
      HelpMessage = "A useful message to display in /xlhelp"
    });

    PluginInterface.UiBuilder.Draw += DrawUI;

    // This adds a button to the plugin installer entry of this plugin which allows
    // to toggle the display status of the configuration ui
    PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

    ECommonsMain.Init(PluginInterface, this);
    _autoPinch = new AutoPinch();
  }

  public void Dispose()
  {
    WindowSystem.RemoveAllWindows();

    ConfigWindow.Dispose();

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
    _autoPinch.Draw();
  }

  public void ToggleConfigUI() => ConfigWindow.Toggle();
}
