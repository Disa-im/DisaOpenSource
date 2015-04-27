using System;
using Disa.Framework;

namespace Disa.Terminal
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            PlatformManager.InitializePlatform(new WindowsMac());
            PlatformManager.InitializeMain(new Service[0]);
            Console.WriteLine("Initialized.");
            Console.ReadLine();

        }
    }
}
