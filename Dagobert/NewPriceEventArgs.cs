using System;

namespace Dagobert
{
  internal class NewPriceEventArgs(uint newPrice) : EventArgs
  {
    public uint NewPrice { get; } = newPrice;
  }
}
