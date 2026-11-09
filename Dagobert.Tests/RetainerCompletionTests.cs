using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using NUnit.Framework;

namespace Dagobert.Tests;

public unsafe class RetainerCompletionTests
{
  [Test]
  public void MissingRetainerListIsAlreadyComplete()
  {
    Assert.That(AutoPinch.CloseRetainerList(null), Is.True);
  }

  [Test]
  public void HiddenRetainerListIsAlreadyComplete()
  {
    var addon = new AtkUnitBase();
    addon.UldManager.LoadedState = AtkLoadState.Loaded;

    Assert.That(AutoPinch.CloseRetainerList(&addon), Is.True);
  }

  [Test]
  public void VisibleLoadingListMustNotCloseOrResumeAutoRetainer()
  {
    var addon = new AtkUnitBase { IsVisible = true };
    addon.UldManager.LoadedState = AtkLoadState.ResourceLoading;

    Assert.That(AutoPinch.CloseRetainerList(&addon), Is.False);
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

    Assert.That(AutoPinch.CloseRetainerList(&addon), Is.False);
    Assert.That(addon.IsVisible, Is.True);
  }

  [TestCase(true)]
  [TestCase(false)]
  public void ClosingListCompletesOnlyAfterItBecomesHidden(bool hideOnClose)
  {
    var close = hideOnClose
      ? (delegate* unmanaged<AtkUnitBase*, byte, byte>)&CloseAndHide
      : (delegate* unmanaged<AtkUnitBase*, byte, byte>)&CloseWithoutHiding;
    var vtable = new AtkUnitBase.AtkUnitBaseVirtualTable
    {
      IsFullyLoaded = (delegate* unmanaged<AtkUnitBase*, bool>)(delegate* unmanaged<AtkUnitBase*, byte>)&FullyLoaded,
      Close = (delegate* unmanaged<AtkUnitBase*, bool, bool>)close,
    };
    var addon = new AtkUnitBase { VirtualTable = &vtable, IsVisible = true };
    addon.UldManager.LoadedState = AtkLoadState.Loaded;

    Assert.That(AutoPinch.CloseRetainerList(&addon), Is.False);
    Assert.That(addon.IsVisible, Is.EqualTo(!hideOnClose));
    Assert.That(AutoPinch.CloseRetainerList(&addon), Is.EqualTo(hideOnClose));
  }

  // Native C++ bools use bytes in these callbacks so their parameters are blittable.
  [UnmanagedCallersOnly]
  private static byte FullyLoaded(AtkUnitBase* addon) => 1;

  [UnmanagedCallersOnly]
  private static byte NotFullyLoaded(AtkUnitBase* addon) => 0;

  [UnmanagedCallersOnly]
  private static byte CloseAndHide(AtkUnitBase* addon, byte fireCallback)
  {
    addon->IsVisible = false;
    return 1;
  }

  [UnmanagedCallersOnly]
  private static byte CloseWithoutHiding(AtkUnitBase* addon, byte fireCallback) => 1;
}
