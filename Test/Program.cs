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
            public Controller() : base("My-Unique-ID")
            {
                
            }

            protected override void OnStartup(StartupEventArgs e)
            {
                Console.WriteLine("Main instance. Waiting for next instance args.");
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
                    Console.WriteLine("Args: " + string.Join(";", e.Arguments));
                }
            }
        }
    }
}
