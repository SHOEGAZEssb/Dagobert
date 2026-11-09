using System.Text.Json;
using NUnit.Framework;

namespace Dagobert.Tests;

public class UndercutLimitTests
{
  [TestCase(20_000_000, 19_500_000, 10f, 500_000, true)]
  [TestCase(20_000_000, 19_499_999, 10f, 500_000, false)]
  [TestCase(100_000, 90_000, 10f, 500_000, true)]
  [TestCase(100_000, 89_999, 10f, 500_000, false)]
  [TestCase(100_000, 49_999, 10f, 500_000, false)]
  [TestCase(100_000, 90_000, 100f, 5_000, false)]
  [TestCase(100_000, 90_000, 10f, 0, true)]
  [TestCase(100_000, 89_999, 10f, 0, false)]
  [TestCase(1_000, 993, 0.7f, 0, true)]
  [TestCase(1_000, 992, 0.7f, 0, false)]
  [TestCase(1_000, 1, 99.9f, 0, true)]
  [TestCase(int.MaxValue, 2_145_336_164, 0.1f, 0, true)]
  [TestCase(int.MaxValue, 2_145_336_163, 0.1f, 0, false)]
  [TestCase(100_000, 1, 100f, 0, true)]
  [TestCase(100_000, 1, 100f, -1, true)]
  [TestCase(100_000, 100_000, 0f, 1, true)]
  [TestCase(100_000, 150_000, 0f, 1, true)]
  [TestCase(100_000, 99_999, 0f, 1, false)]
  [TestCase(int.MaxValue, 1, 100f, int.MaxValue, true)]
  [TestCase(int.MaxValue, 1, 100f, 500_000, false)]
  [TestCase(16_777_217, 16_777_215, 100f, 1, false)]
  [TestCase(0, 100, 10f, 1, true)]
  public void BothUndercutLimitsMustBeSatisfied(int currentPrice, int price, float percentageLimit, int gilLimit, bool expected)
  {
    var config = new Configuration { MaxUndercutPercentage = percentageLimit, MaxUndercutAmount = gilLimit };

    Assert.That(config.IsUndercutAllowed(currentPrice, price), Is.EqualTo(expected));
  }

  [Test]
  public void ExistingConfigurationsHaveNoGilLimit()
  {
    var config = JsonSerializer.Deserialize<Configuration>("{}")!;

    Assert.That(config.MaxUndercutAmount, Is.Zero);
    Assert.That(config.IsUndercutAllowed(20_000_000, 1), Is.True);
  }

  [Test]
  public void GilLimitSurvivesSerialization()
  {
    var config = new Configuration { MaxUndercutPercentage = 10, MaxUndercutAmount = 500_000 };

    var restored = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;

    Assert.That(restored.MaxUndercutAmount, Is.EqualTo(500_000));
    Assert.That(restored.IsUndercutAllowed(20_000_000, 19_499_999), Is.False);
  }

  [Test]
  public void SafetyCheckUsesTheFinalPriceAfterPerItemLimits()
  {
    var config = new Configuration { MaxUndercutPercentage = 10, MaxUndercutAmount = 500_000 };
    config.ItemPriceLimits.Add(new ItemPriceLimit { ItemId = 1, MaxPrice = 19_000_000 });

    var price = config.ApplyPriceLimits(1, 20_000_000, 19_999_999);

    Assert.That(config.IsUndercutAllowed(20_000_000, price), Is.False);
  }
}
