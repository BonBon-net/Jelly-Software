using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jelly_Software.Tools;

namespace Jelly_Software
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ImdbService.TVShowMain().Wait();
        }
    }
}
 