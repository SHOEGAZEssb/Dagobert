using NUnit.Framework;

namespace Dagobert.Tests;

public class UniversalisPriceTests
{
  [TestCase(1_000L, false, false, UndercutMode.FixedAmount, 1, 999)]
  [TestCase(1_000L, true, false, UndercutMode.FixedAmount, 10, 1_000)]
  [TestCase(1_000L, true, true, UndercutMode.FixedAmount, 10, 990)]
  [TestCase(1_000L, false, false, UndercutMode.Percentage, 10, 900)]
  [TestCase(1_000L, true, false, UndercutMode.Percentage, 10, 1_000)]
  [TestCase(1_000L, true, true, UndercutMode.Percentage, 10, 900)]
  [TestCase(101L, false, false, UndercutMode.Percentage, 1, 99)]
  [TestCase(3L, false, false, UndercutMode.Percentage, 99, 1)]
  [TestCase(1L, false, false, UndercutMode.FixedAmount, 1, 1)]
  [TestCase(10L, false, false, UndercutMode.FixedAmount, 100, 1)]
  [TestCase((long)int.MaxValue, false, false, UndercutMode.FixedAmount, 1, 2_147_483_646)]
  [TestCase(long.MaxValue, false, false, UndercutMode.FixedAmount, 1, 2_147_483_646)]
  [TestCase(long.MaxValue, true, false, UndercutMode.FixedAmount, 1, int.MaxValue)]
  [TestCase(long.MaxValue, false, false, UndercutMode.Percentage, 1, 2_126_008_810)]
  [TestCase(long.MaxValue, false, false, UndercutMode.Percentage, 99, 21_474_836)]
  public void CalculatesUndercutsWithoutGoingBelowOneOrOverflowing(long price, bool ownRetainer, bool undercutSelf, UndercutMode mode, int amount, int expected)
  {
    var config = new Configuration { UndercutSelf = undercutSelf, UndercutMode = mode, UndercutAmount = amount };

    Assert.That(UniversalisPriceProvider.CalculateNewPrice(price, ownRetainer, config), Is.EqualTo(expected));
  }
}
