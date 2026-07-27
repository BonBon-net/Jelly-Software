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
