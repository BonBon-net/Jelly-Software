using Jelly_Software.Tools;

namespace Jelly_Software
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Version 1.0");
            ImdbService.TVShowMain().Wait();
        }
    }
}
