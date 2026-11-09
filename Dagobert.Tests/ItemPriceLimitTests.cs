using NUnit.Framework;

namespace Dagobert.Tests;

public class ItemPriceLimitTests
{
  [TestCase(0, 0, 123, 123)]
  [TestCase(100, 0, 50, 100)]
  [TestCase(100, 0, 150, 150)]
  [TestCase(0, 200, 250, 200)]
  [TestCase(0, 200, 150, 150)]
  [TestCase(100, 200, 100, 100)]
  [TestCase(100, 200, 200, 200)]
  [TestCase(100, 200, 150, 150)]
  [TestCase(-100, -200, 150, 150)]
  [TestCase(-100, 200, 250, 200)]
  [TestCase(100, -200, 50, 100)]
  [TestCase(200, 100, 50, 200)]
  [TestCase(200, 100, 300, 200)]
  [TestCase(int.MaxValue, 0, 1, int.MaxValue)]
  [TestCase(0, 1, int.MaxValue, 1)]
  public void ApplyEnforcesBoundsAndHandlesInvalidSavedLimits(int minimum, int maximum, int price, int expected)
  {
    var limit = new ItemPriceLimit { MinPrice = minimum, MaxPrice = maximum };

    Assert.That(limit.Apply(price), Is.EqualTo(expected));
  }
}
