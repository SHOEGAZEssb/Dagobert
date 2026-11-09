using System.Text.Json;
using NUnit.Framework;

namespace Dagobert.Tests;

public class PriceIncreaseLimitTests
{
  [TestCase(100_000, 500_000, 50f, true, 100_000)]
  [TestCase(100_000, 150_000, 50f, true, 150_000)]
  [TestCase(100_000, 150_001, 50f, true, 100_000)]
  [TestCase(100_000, 149_999, 50f, true, 149_999)]
  [TestCase(100_000, 99_999, 50f, true, 99_999)]
  [TestCase(100_000, 100_000, 50f, true, 100_000)]
  [TestCase(100_000, 500_000, 0f, true, 500_000)]
  [TestCase(100_000, 150_000, 50f, false, 100_000)]
  [TestCase(100_000, 500_000, 0f, false, 100_000)]
  [TestCase(100_000, 99_999, 50f, false, 99_999)]
  [TestCase(1_000, 1_007, 0.7f, true, 1_007)]
  [TestCase(1_000, 1_008, 0.7f, true, 1_000)]
  [TestCase(1, 2, 50f, true, 1)]
  [TestCase(1, 2, 100f, true, 2)]
  [TestCase(100, 2_100, 2_000f, true, 2_100)]
  [TestCase(100, 2_101, 2_000f, true, 100)]
  [TestCase(0, 100, 50f, true, 0)]
  [TestCase(0, 100, 0f, true, 100)]
  [TestCase(1, int.MaxValue, 50f, true, 1)]
  [TestCase(2_000_000_000, int.MaxValue, 10f, true, int.MaxValue)]
  public void ExcessiveIncreasesKeepTheCurrentPrice(int currentPrice, int candidatePrice, float maximum, bool allowIncreases, int expected)
  {
    var config = new Configuration { AllowPriceIncreases = allowIncreases, MaxPriceIncreasePercentage = maximum };

    Assert.That(config.ApplyPriceLimits(1, currentPrice, candidatePrice), Is.EqualTo(expected));
  }

  [Test]
  public void ExistingConfigurationsHaveUnlimitedPriceIncreases()
  {
    var config = JsonSerializer.Deserialize<Configuration>("{}")!;

    Assert.That(config.MaxPriceIncreasePercentage, Is.Zero);
    Assert.That(config.ApplyPriceLimits(1, 1, int.MaxValue), Is.EqualTo(int.MaxValue));
  }

  [Test]
  public void IncreaseLimitSurvivesSerialization()
  {
    var config = new Configuration { MaxPriceIncreasePercentage = 50 };

    var restored = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;

    Assert.That(restored.MaxPriceIncreasePercentage, Is.EqualTo(50));
    Assert.That(restored.ApplyPriceLimits(1, 100_000, 500_000), Is.EqualTo(100_000));
  }

  [Test]
  public void PerItemMinimumCannotExceedTheIncreaseLimit()
  {
    var config = new Configuration { MaxPriceIncreasePercentage = 50 };
    config.ItemPriceLimits.Add(new ItemPriceLimit { ItemId = 1, MinPrice = 200_000 });

    Assert.That(config.ApplyPriceLimits(1, 100_000, 99_999), Is.EqualTo(100_000));
  }

  [Test]
  public void PerItemMaximumCanBringAnIncreaseWithinTheLimit()
  {
    var config = new Configuration { MaxPriceIncreasePercentage = 50 };
    config.ItemPriceLimits.Add(new ItemPriceLimit { ItemId = 1, MaxPrice = 150_000 });

    Assert.That(config.ApplyPriceLimits(1, 100_000, 500_000), Is.EqualTo(150_000));
  }

  [Test]
  public void SameCandidateIsCheckedAgainstEachListingsCurrentPrice()
  {
    var config = new Configuration { MaxPriceIncreasePercentage = 50 };

    Assert.That(config.ApplyPriceLimits(1, 100_000, 500_000), Is.EqualTo(100_000));
    Assert.That(config.ApplyPriceLimits(1, 400_000, 500_000), Is.EqualTo(500_000));
  }
}
