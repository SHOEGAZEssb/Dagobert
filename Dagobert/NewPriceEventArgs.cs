using System;

namespace Dagobert
{
  internal class NewPriceEventArgs(int newPrice) : EventArgs
  {
    public int NewPrice { get; } = newPrice;
  }
}
