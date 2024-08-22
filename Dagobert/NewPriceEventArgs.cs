using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dagobert
{
  internal class NewPriceEventArgs(uint newPrice) : EventArgs
  {
    public uint NewPrice { get; } = newPrice;
  }
}
