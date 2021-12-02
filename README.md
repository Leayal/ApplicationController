# Application controller
Provides a simple skeleton application to manage instances for single-instance application (support command-line application).

It's here because of the `It's not worth to include the whole System.VisualBasic reference just for single-instance application` reason.

I don't think it's bug-free, though. So fire any bug reports if you want to.

## Note
* While it's tested on Windows platform. I don't know whether it's running well on other platforms.
* Unlike [Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.applicationservices.windowsformsapplicationbase?view=netframework-4.0). The class has slightly different implementation:
  * Run(string[]) or Run() will then invoke either one of these abstract methods in the devired classes:
    * OnStartupFirstInstance(string[]) if it is the instance is launched for the first time.
	* OnStartupNextInstance(int, string[]) if it is the instance is launched after the first time.
  * This means you will implement Windows message loop by yourself.
* Using:
  * The class use [NamedPipe](https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication) to pass the command-line arguments from subsequent instances to the first instance.
  * The class use [Mutex](https://docs.microsoft.com/en-us/dotnet/standard/threading/mutexes) to determine the first instance and subsequent instances.

## Example:
See the live-action by compile and launch `Test` project, or use the example below:
```csharp
using System;
using Leayal.ApplicationController;

class Program
{
    // Our usual application's entry point here. If the entry point configuration is not changed.
    static void Main(string[] args)
    {
	    // Ensure the Controller instance is disposed to release Mutex and Pipes created by the instance.
        using (var controller = new Controller())
        {
            controller.Run(args);
        }

        // Your can even use "controller.Run();".
        // That method implies that the application is running without arguments.
    }

    // Derived class from the base class
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
```

# Developer Note
As of writing this project. I'm using `Visual Studio 2022`.