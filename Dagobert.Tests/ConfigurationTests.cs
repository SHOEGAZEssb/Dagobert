using System.Text.Json;
using NUnit.Framework;

namespace Dagobert.Tests;

public class ConfigurationTests
{
  [Test]
  public void AddingAnItemTwicePreservesItsExistingLimits()
  {
    var config = new Configuration();
    var original = config.GetOrAddItemPriceLimit(10);
    original.MinPrice = 100;
    original.MaxPrice = 200;

    var existing = config.GetOrAddItemPriceLimit(10);

    Assert.That(existing, Is.SameAs(original));
    Assert.That(existing.MinPrice, Is.EqualTo(100));
    Assert.That(existing.MaxPrice, Is.EqualTo(200));
    Assert.That(config.ItemPriceLimits, Has.Count.EqualTo(1));
  }

  [Test]
  public void DifferentItemsKeepIndependentLimits()
  {
    var config = new Configuration();
    config.GetOrAddItemPriceLimit(10).MinPrice = 100;
    config.GetOrAddItemPriceLimit(20).MinPrice = 200;

    Assert.That(config.GetItemPriceLimit(10)!.MinPrice, Is.EqualTo(100));
    Assert.That(config.GetItemPriceLimit(20)!.MinPrice, Is.EqualTo(200));
    Assert.That(config.ItemPriceLimits, Has.Count.EqualTo(2));
  }

  [Test]
  public void LookingUpAnUnknownItemDoesNotCreateALimit()
  {
    var config = new Configuration();

    Assert.That(config.GetItemPriceLimit(10), Is.Null);
    Assert.That(config.ItemPriceLimits, Is.Empty);
  }

  [TestCase("Retainer A")]
  [TestCase(Configuration.ALL_DISABLED_SENTINEL)]
  public void RetainerSelectionsAndItemLimitsSurviveSerialization(string selectedRetainer)
  {
    var config = new Configuration();
    config.EnabledRetainerNames.Add(selectedRetainer);
    config.LastKnownRetainerNames.Add("Retainer A");
    config.SeenRetainers.Add(ulong.MaxValue);
    config.ItemPriceLimits.Add(new ItemPriceLimit { ItemId = 10, MinPrice = 100, MaxPrice = 200 });

    var restored = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;

    Assert.That(restored.EnabledRetainerNames, Is.EquivalentTo(config.EnabledRetainerNames));
    Assert.That(restored.LastKnownRetainerNames, Is.EqualTo(config.LastKnownRetainerNames));
    Assert.That(restored.SeenRetainers, Is.EqualTo(config.SeenRetainers));
    Assert.That(restored.GetItemPriceLimit(10)!.Apply(50), Is.EqualTo(100));
    Assert.That(restored.GetItemPriceLimit(10)!.Apply(250), Is.EqualTo(200));
  }
}
