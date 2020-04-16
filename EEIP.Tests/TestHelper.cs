using System;
using System.Collections.Generic;
using System.Text;

namespace EEIP.Tests
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
