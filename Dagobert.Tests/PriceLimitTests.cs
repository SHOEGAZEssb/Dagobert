using System.Text.Json;
using NUnit.Framework;

namespace Dagobert.Tests;

public class PriceLimitTests
{
  [Test]
  public void ExistingConfigurationsAllowPriceIncreases()
  {
    var config = JsonSerializer.Deserialize<Configuration>("{}")!;

    Assert.That(config.AllowPriceIncreases, Is.True);
    Assert.That(config.ApplyPriceLimits(1, 100_000, 499_999), Is.EqualTo(499_999));
  }

  [TestCase(1u, 100_000, 499_999, 100_000)]
  [TestCase(1u, 100_000, 99_999, 99_999)]
  [TestCase(1u, 100_000, 100_000, 100_000)]
  [TestCase(1u, 1, int.MaxValue, 1)]
  [TestCase(1u, 0, 100, 0)]
  [TestCase(0u, 100_000, 499_999, 100_000)]
  [TestCase(1u, 300_000, 499_999, 300_000)]
  public void DisablingIncreasesCapsPricesAtEachListingsAskingPrice(uint itemId, int currentPrice, int candidatePrice, int expected)
  {
    var config = new Configuration { AllowPriceIncreases = false };

    Assert.That(config.ApplyPriceLimits(itemId, currentPrice, candidatePrice), Is.EqualTo(expected));
  }

  [TestCase(false, 100_000, 90_000, 100_000)]
  [TestCase(false, 300_000, 499_999, 200_000)]
  [TestCase(false, 300_000, 90_000, 150_000)]
  [TestCase(true, 100_000, 90_000, 150_000)]
  [TestCase(true, 100_000, 499_999, 200_000)]
  public void PerItemLimitsRespectThePriceIncreaseSetting(bool allowIncreases, int currentPrice, int candidatePrice, int expected)
  {
    var config = new Configuration { AllowPriceIncreases = allowIncreases };
    config.ItemPriceLimits.Add(new ItemPriceLimit { ItemId = 1, MinPrice = 150_000, MaxPrice = 200_000 });

    Assert.That(config.ApplyPriceLimits(1, currentPrice, candidatePrice), Is.EqualTo(expected));
  }

  [TestCase(false)]
  [TestCase(true)]
  public void PriceIncreaseSettingSurvivesSerialization(bool allowIncreases)
  {
    var config = new Configuration { AllowPriceIncreases = allowIncreases };

    var restored = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;

    Assert.That(restored.AllowPriceIncreases, Is.EqualTo(allowIncreases));
    Assert.That(restored.ApplyPriceLimits(1, 100_000, 499_999), Is.EqualTo(allowIncreases ? 499_999 : 100_000));
  }
}
