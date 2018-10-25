using System;
using System.Collections.ObjectModel;

namespace Leayal.ApplicationController
{
    /// <summary>
    /// Provides data for the <see cref="ApplicationBase.OnStartup(StartupEventArgs)"/> event.
    /// </summary>
    public sealed class StartupEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the command-line arguments of the application
        /// </summary>
        public ReadOnlyCollection<string> Arguments { get; }

        internal StartupEventArgs(string[] args) : base()
        {
            this.Arguments = new ReadOnlyCollection<string>(args);
        }
    }

    /// <summary>
    /// Provides data for the <see cref="ApplicationBase.OnStartupNextInstance(StartupNextInstanceEventArgs)"/> event.
    /// </summary>
    public sealed class StartupNextInstanceEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the command-line arguments of the subsequent application instance
        /// </summary>
        public ReadOnlyCollection<string> Arguments { get; }

        /// <summary>
        /// Get the exception when trying to parse the argument.
        /// </summary>
        public Exception Error { get; }
        internal StartupNextInstanceEventArgs(Exception ex) : base()
        {
            this.Arguments = null;
            this.Error = ex;
        }

        internal StartupNextInstanceEventArgs(string[] args) : base()
        {
            this.Error = null;
            this.Arguments = new ReadOnlyCollection<string>(args);
        }
    }
}
