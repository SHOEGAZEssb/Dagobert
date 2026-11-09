using NUnit.Framework;

namespace Dagobert.Tests;

public class PinchHotkeyTests
{
  [TestCase(false, false, false)]
  [TestCase(false, true, true)]
  [TestCase(true, true, false)]
  [TestCase(true, false, false)]
  public void OnlyANewKeyPressTriggersAnAction(bool wasDown, bool keyDown, bool expected)
  {
    Assert.That(AutoPinch.ConsumePinchKeyPress(keyDown, ref wasDown), Is.EqualTo(expected));
    Assert.That(wasDown, Is.EqualTo(keyDown));
  }

  [Test]
  public void HeldKeyTriggersOnlyOnceUntilReleased()
  {
    var wasDown = false;

    Assert.That(AutoPinch.ConsumePinchKeyPress(true, ref wasDown), Is.True);
    Assert.That(AutoPinch.ConsumePinchKeyPress(true, ref wasDown), Is.False);
    Assert.That(AutoPinch.ConsumePinchKeyPress(true, ref wasDown), Is.False);
    Assert.That(AutoPinch.ConsumePinchKeyPress(false, ref wasDown), Is.False);
    Assert.That(AutoPinch.ConsumePinchKeyPress(true, ref wasDown), Is.True);
    Assert.That(AutoPinch.ConsumePinchKeyPress(true, ref wasDown), Is.False);
  }
}
