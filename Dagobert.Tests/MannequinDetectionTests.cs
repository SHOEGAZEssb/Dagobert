using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace Dagobert.Tests;

public class MannequinDetectionTests
{
  [TestCase("Adjust Price", "Adjust Price")]
  [TestCase("Preis ändern", "ANGEBOT ÄNDERN")]
  [TestCase("Changer le prix", "Ajuster le prix")]
  [TestCase("価格を変更する", "EDIT PRICE")]
  public void NormalListingsUseTheMenuActionRowInsteadOfTheWindowTitle(string menuLabel, string windowTitle)
  {
    // Actual Addon sheet text: the English title happens to match the menu action.
    var addonText = new Dictionary<uint, string> { [930] = windowTitle, [959] = menuLabel };

    Assert.That(AutoPinch.IsItemMannequin([menuLabel, "Remove"], addonText[AutoPinch.AdjustPriceMenuTextRowId]), Is.False);
  }

  [TestCase("Adjust Price")]
  [TestCase("Preis ändern")]
  [TestCase("価格を変更する")]
  [TestCase("Changer le prix")]
  [TestCase("调整价格")]
  [TestCase("Another localized price action")]
  public void NormalListingsAreRecognizedUsingTheClientsMenuText(string localizedLabel)
  {
    Assert.That(AutoPinch.IsItemMannequin([localizedLabel, "Remove"], localizedLabel), Is.False);
  }

  [TestCase("Display on mannequin")]
  [TestCase("Remove from mannequin")]
  [TestCase("Do not Adjust Price")]
  [TestCase("")]
  public void MenusWithoutThePriceActionRemainProtected(string menuEntry)
  {
    Assert.That(AutoPinch.IsItemMannequin([menuEntry], "Adjust Price"), Is.True);
  }

  [Test]
  public void EmptyMenusRemainProtected()
  {
    Assert.That(AutoPinch.IsItemMannequin(Array.Empty<string>(), "调整价格"), Is.True);
  }

  [TestCase(null)]
  [TestCase("")]
  [TestCase(" ")]
  public void MissingLocalizedLabelDoesNotMatchAnEmptyMenuEntry(string? localizedLabel)
  {
    Assert.That(AutoPinch.IsItemMannequin(["", " "], localizedLabel!), Is.True);
  }

  [Test]
  public void MenuMatchingIsIndependentOfTheOperatingSystemCulture()
  {
    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

      Assert.That(AutoPinch.IsItemMannequin(["ADJUST PRICE"], "Adjust Price"), Is.False);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
