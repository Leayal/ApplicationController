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

        class Controller : ApplicationController
        {
            public Controller() : base() { }

            protected override void OnStartupFirstInstance(string[] args)
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Main instance. Launched without args.");
                }
                else
                {
                    Console.WriteLine("Main instance. Launched with args: " + string.Join(";", args));
                }
                Console.WriteLine("Waiting for subsequent instance args. Press any key to exit.");
                Console.ReadKey();
            }

            protected override void OnStartupNextInstance(int processId, string[] args)
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Subsequent Process launched without args.");
                }
                else
                {
                    Console.WriteLine("Subsequent Process launched with args: " + string.Join(";", args));
                }
            }
        }
    }
}
