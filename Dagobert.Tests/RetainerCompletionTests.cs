using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using NUnit.Framework;

namespace Dagobert.Tests;

[NonParallelizable]
public unsafe class RetainerCompletionTests
{
  private nint _previousFireCallback;
  private static int _callbackCount;
  private static uint _valueCount;
  private static AtkValue _callbackValue;
  private static bool _closeFlag;
  private static bool _hideOnCallback;

  [SetUp]
  public void SetUp()
  {
    _previousFireCallback = AtkUnitBase.Addresses.FireCallback.Value;
    AtkUnitBase.Addresses.FireCallback.Value = (nint)(delegate* unmanaged<AtkUnitBase*, uint, AtkValue*, byte, byte>)&FireCallback;
    _callbackCount = 0;
    _valueCount = 0;
    _callbackValue = default;
    _closeFlag = false;
    _hideOnCallback = false;
  }

  [TearDown]
  public void TearDown() => AtkUnitBase.Addresses.FireCallback.Value = _previousFireCallback;

  [TestCase(false)]
  [TestCase(true)]
  public void MissingRetainerListCompletesOnlyAfterCloseWasRequested(bool closeRequested)
  {
    Assert.That(AutoPinch.CloseRetainerList(null, ref closeRequested), Is.EqualTo(closeRequested));
  }

  [TestCase(false)]
  [TestCase(true)]
  public void HiddenRetainerListCompletesOnlyAfterCloseWasRequested(bool closeRequested)
  {
    var addon = new AtkUnitBase();
    addon.UldManager.LoadedState = AtkLoadState.Loaded;

    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.EqualTo(closeRequested));
    Assert.That(_callbackCount, Is.Zero);
  }

  [Test]
  public void VisibleLoadingListMustNotCloseOrResumeAutoRetainer()
  {
    var addon = new AtkUnitBase { IsVisible = true };
    addon.UldManager.LoadedState = AtkLoadState.ResourceLoading;
    bool closeRequested = false;

    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.False);
    Assert.That(closeRequested, Is.False);
    Assert.That(_callbackCount, Is.Zero);
    Assert.That(addon.IsVisible, Is.True);
  }

  [Test]
  public void PartiallyLoadedListMustNotCloseOrResumeAutoRetainer()
  {
    var vtable = new AtkUnitBase.AtkUnitBaseVirtualTable
    {
      IsFullyLoaded = (delegate* unmanaged<AtkUnitBase*, bool>)(delegate* unmanaged<AtkUnitBase*, byte>)&NotFullyLoaded,
    };
    var addon = new AtkUnitBase { VirtualTable = &vtable, IsVisible = true };
    addon.UldManager.LoadedState = AtkLoadState.Loaded;
    bool closeRequested = false;

    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.False);
    Assert.That(closeRequested, Is.False);
    Assert.That(_callbackCount, Is.Zero);
    Assert.That(addon.IsVisible, Is.True);
  }

  [TestCase(true)]
  [TestCase(false)]
  public void WaitsForListToReturnThenSendsCloseCallbackAndWaitsForItToDisappear(bool hideOnClose)
  {
    _hideOnCallback = hideOnClose;
    var vtable = new AtkUnitBase.AtkUnitBaseVirtualTable
    {
      IsFullyLoaded = (delegate* unmanaged<AtkUnitBase*, bool>)(delegate* unmanaged<AtkUnitBase*, byte>)&FullyLoaded,
    };
    var addon = new AtkUnitBase { VirtualTable = &vtable };
    addon.UldManager.LoadedState = AtkLoadState.Loaded;
    bool closeRequested = false;

    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.False);
    Assert.That(_callbackCount, Is.Zero);

    addon.IsVisible = true;
    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.False);
    Assert.That(closeRequested, Is.True);
    Assert.That(_callbackCount, Is.EqualTo(1));
    Assert.That(_valueCount, Is.EqualTo(1));
    Assert.That(_callbackValue.Type, Is.EqualTo(AtkValueType.Int));
    Assert.That(_callbackValue.Int, Is.EqualTo(-1));
    Assert.That(_closeFlag, Is.False);
    Assert.That(addon.IsVisible, Is.EqualTo(!hideOnClose));
    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.EqualTo(hideOnClose));
    Assert.That(_callbackCount, Is.EqualTo(1));

    addon.IsVisible = false;
    Assert.That(AutoPinch.CloseRetainerList(&addon, ref closeRequested), Is.True);
  }

  // Native C++ bools use bytes in these callbacks so their parameters are blittable.
  [UnmanagedCallersOnly]
  private static byte FullyLoaded(AtkUnitBase* addon) => 1;

  [UnmanagedCallersOnly]
  private static byte NotFullyLoaded(AtkUnitBase* addon) => 0;

  [UnmanagedCallersOnly]
  private static byte FireCallback(AtkUnitBase* addon, uint valueCount, AtkValue* values, byte close)
  {
    _callbackCount++;
    _valueCount = valueCount;
    _callbackValue = values[0];
    _closeFlag = close != 0;
    if (_hideOnCallback)
      addon->IsVisible = false;
    return 1;
  }
}
