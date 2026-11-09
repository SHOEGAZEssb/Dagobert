using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using NUnit.Framework;

namespace Dagobert.Tests;

[NonParallelizable]
public class ManualPostingTests
{
  private AutoPinch _pinch = null!;
  private TaskManager _tasks = null!;
  private IDictionary _cachedPrices = null!;
  private CancellationTokenSource _pendingRequest = null!;
  private CancellationToken _pendingToken;
  private IFramework? _previousFramework;
  private Configuration? _previousConfiguration;
  private IKeyState? _previousKeyState;

  [SetUp]
  public void SetUp()
  {
    _previousFramework = Svc.Framework;
    _previousConfiguration = Plugin.Configuration;
    _previousKeyState = Plugin.KeyState;
    typeof(Svc).GetProperty(nameof(Svc.Framework))!.SetValue(null, DispatchProxy.Create<IFramework, FrameworkEventsProxy>());
    typeof(Plugin).GetProperty(nameof(Plugin.Configuration))!.SetValue(null, new Configuration { EnablePostPinchkey = false });
    _tasks = new TaskManager { ShowDebug = false };

    // The normal constructor needs live game services; exercise the real handler with an isolated queue.
    _pinch = (AutoPinch)RuntimeHelpers.GetUninitializedObject(typeof(AutoPinch));
    SetField("_taskManager", _tasks);
    var cacheType = typeof(AutoPinch).GetField("_cachedPrices", BindingFlags.Instance | BindingFlags.NonPublic)!.FieldType;
    _cachedPrices = (IDictionary)Activator.CreateInstance(cacheType)!;
    var priceType = typeof(AutoPinch).GetNestedType("CachedPrice", BindingFlags.NonPublic)!;
    _cachedPrices.Add("Waterfall Partition", Activator.CreateInstance(priceType, 480, false));
    SetField("_cachedPrices", _cachedPrices);
    SetField("_newPrice", 480);
    SetField("_universalisPriceRequestId", 7);
    _pendingRequest = new CancellationTokenSource();
    _pendingToken = _pendingRequest.Token;
    SetField("_universalisPriceRequestCts", _pendingRequest);
  }

  [TearDown]
  public void TearDown()
  {
    try
    {
      ((IDisposable)_tasks).Dispose();
      _pendingRequest.Dispose();
    }
    finally
    {
      typeof(Svc).GetProperty(nameof(Svc.Framework))!.SetValue(null, _previousFramework);
      typeof(Plugin).GetProperty(nameof(Plugin.Configuration))!.SetValue(null, _previousConfiguration);
      typeof(Plugin).GetProperty(nameof(Plugin.KeyState))!.SetValue(null, _previousKeyState);
    }
  }

  [TestCase(false)]
  [TestCase(true)]
  public void OpeningAManualListingDiscardsThePreviousPriceAndPendingRequest(bool useUniversalis)
  {
    Plugin.Configuration.UseUniversalisDataCenterPrices = useUniversalis;

    OpenSellWindow();

    Assert.That(GetField<IDictionary>("_cachedPrices"), Is.Empty);
    Assert.That(GetField<int?>("_newPrice"), Is.Null);
    Assert.That(_pendingToken.IsCancellationRequested, Is.True);
    Assert.That(GetField<int>("_universalisPriceRequestId"), Is.EqualTo(8));
    Assert.That(_tasks.IsBusy, Is.False);
  }

  [Test]
  public void ManualPostHotkeyStartsAFreshPriceCheck()
  {
    Plugin.Configuration.EnablePostPinchkey = true;
    typeof(Plugin).GetProperty(nameof(Plugin.KeyState))!.SetValue(null, DispatchProxy.Create<IKeyState, HeldKeyProxy>());

    OpenSellWindow();

    Assert.That(GetField<IDictionary>("_cachedPrices"), Is.Empty);
    Assert.That(_tasks.TaskStack, Does.Contain("ClickComparePricePosted"));
    Assert.That(_tasks.TaskStack, Does.Contain("SetNewPricePosted"));
  }

  [Test]
  public void OpeningTheNextListingInABatchPreservesItsPriceCache()
  {
    _tasks.Enqueue(() => true, "Running batch");

    OpenSellWindow();

    Assert.That(GetField<IDictionary>("_cachedPrices"), Is.SameAs(_cachedPrices));
    Assert.That(_cachedPrices.Contains("Waterfall Partition"), Is.True);
    Assert.That(GetField<int?>("_newPrice"), Is.EqualTo(480));
    Assert.That(_pendingToken.IsCancellationRequested, Is.False);
    Assert.That(GetField<int>("_universalisPriceRequestId"), Is.EqualTo(7));
    Assert.That(_tasks.NumQueuedTasks, Is.EqualTo(1));
  }

  private void OpenSellWindow() => typeof(AutoPinch)
    .GetMethod("RetainerSellPostSetup", BindingFlags.Instance | BindingFlags.NonPublic)!
    .Invoke(_pinch, [AddonEvent.PostSetup, null]);

  private void SetField(string name, object? value) => typeof(AutoPinch)
    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_pinch, value);

  private T GetField<T>(string name) => (T)typeof(AutoPinch)
    .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_pinch)!;

  public class FrameworkEventsProxy : DispatchProxy
  {
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
      targetMethod?.Name is "add_Update" or "remove_Update" ? null : throw new NotSupportedException(targetMethod?.Name);
  }

  public class HeldKeyProxy : DispatchProxy
  {
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
      targetMethod?.Name == "get_Item" ? true : throw new NotSupportedException(targetMethod?.Name);
  }
}
