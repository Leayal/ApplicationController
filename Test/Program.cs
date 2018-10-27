using System;
using Leayal.ApplicationController;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller();
            controller.Run(args);
        }

        class Controller : ApplicationBase
        {
#if NETCOREAPP1_0 || NETCOREAPP1_1 || NETCOREAPP2_0 || NETCOREAPP2_1
            public Controller() : base("My-Unique-ID") { }
#else
            public Controller() : base(true) { }
#endif

            protected override void OnStartup(StartupEventArgs e)
            {
                if (e.Arguments.Count == 0)
                {
                    Console.WriteLine("Main instance. Launched without args.");
                }
                else
                {
                    Console.WriteLine("Main instance. Launched with args: " + string.Join(";", e.Arguments));
                }
                Console.WriteLine("Waiting for subsequent instance args. Press any key to exit.");
                Console.ReadKey();
            }

            protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
            {
                if (e.Error != null)
                {
                    Console.WriteLine("Error: " + e.Error.Message);
                }
                else
                {
                    if (e.Arguments.Count == 0)
                    {
                        Console.WriteLine("Subsequent Process launched without args.");
                    }
                    else
                    {
                        Console.WriteLine("Subsequent Process launched with args: " + string.Join(";", e.Arguments));
                    }
                }
            }
        }
    }
}
