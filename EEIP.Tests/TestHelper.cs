using System;
using System.Collections.Generic;
using System.Text;

namespace Sres.Net.EEIP.Tests
{
    public class TestHelper
    {
        public static int CreateRandomSeed()
        {
            int seed = new Random().Next();
            Console.WriteLine($"Using random seed {seed}");
            return seed;
        }
    }
}
