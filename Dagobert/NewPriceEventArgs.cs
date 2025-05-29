using System;

namespace Dagobert
{
  internal sealed class NewPriceEventArgs(int newPrice) : EventArgs
  {
    public int NewPrice { get; } = newPrice;
  }
}
