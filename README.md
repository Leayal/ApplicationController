# Application controller
Provides a simple skeleton application to manage instances (Single-instance or multi-instance, support command-line).
It's here because of the `It's not worth to include the whole System.VisualBasic reference just for single-instance application` reason.
It's not bug-free, though.

## Caveat
* The method executing order is different from [Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.applicationservices.windowsformsapplicationbase?view=netframework-4.0). The order of this class is:
  1. Run(string[]) or Run()
  2. OnRun(string[])
  3. OnStartup(StartupEventArgs)
  4. OnStartupNextInstance(StartupNextInstanceEventArgs) (in case the application is in single-instance and a subsequent instance has been launched after)
* In case of single-instance:
  * The class use [MemoryMappedFile](https://docs.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedfile?view=netframework-4.0) and [Mutexes](https://docs.microsoft.com/en-us/dotnet/standard/threading/mutexes) to lock and pass the command-line arguments from subsequent instances to the first instance.
  * The class use [Mutexes](https://docs.microsoft.com/en-us/dotnet/standard/threading/mutexes) to determine the first instance and subsequent instances.

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
        Controller controller = new Controller();
        controller.Run(args);

        // Your can even use "controller.Run();".
        // That method will try to get the command-line arguments from System.Environment.GetCommandLineArgs().
        // But that may not working well.
    }

    // Derived class from the base class
    class Controller : ApplicationBase
    {
        // Invoke constructor ApplicationBase(string)
        // ApplicationBase("My-unique-ID") means create single-instance application model with the given instance ID string, which is "My-unique-ID".
        public Controller() : base("My-unique-ID")
        {                
        }

        // Override ApplicationBase.Startup(StartupEventArgs e)
        // Just for example: Print the line and wait for any key to exit
        protected override void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine("Main instance. Waiting for next instance args.");
            Console.ReadKey();
        }

        // Override OnStartupNextInstance(StartupNextInstanceEventArgs e)
        // Just for example: a subsequent instance has been launched. This method will be invoked with the command-line argument(s) of that subsequent instance.
        // REMARK: this method will never be invoked if the application is not in single-instance model.
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
```

## Unsure how it works so that you can use it or not?
Please browse the source code here: [ApplicationBase.cs](\Src\ApplicationBase.cs)