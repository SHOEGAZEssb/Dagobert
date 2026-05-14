using System;
using System.Linq;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;

namespace Dagobert
{
  public class AutoRetainerIPC
  {
    public const string Name = "AutoRetainer";
    public static bool Installed => Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == Name && x.IsLoaded);

    private static AutoRetainerIPC? _instance;
    public static AutoRetainerIPC? Instance => _instance;

    #nullable disable
    [EzIPC] public readonly Func<bool> GetSuppressed;
    [EzIPC] public readonly Action<bool> SetSuppressed;
    #nullable enable

    public static bool Suppressed() => _instance != null && _instance.GetSuppressed();
    public static bool Suppressed(bool value)
    {
      Svc.Log.Debug($"AR Suppressed={value}");
      _instance?.SetSuppressed(value);
      return true;
    }

    private AutoRetainerIPC() => EzIPC.Init(this, Name);

    public static void Initialize()
    {
      if (Installed)
        _instance = new AutoRetainerIPC();
    }

    public static void Dispose()
    {
      _instance = null;
    }
  }
}
