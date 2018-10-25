using System;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Leayal.ApplicationController
{
    /// <summary>
    /// Base class provides skeleton for application controlling. If it's in single-instance model, the class will use <see cref="MemoryMappedFile"/> to pass the command-line from subsequent instances to the first instance.
    /// </summary>
    /// <remarks>
    /// The model run in this order: <see cref="Run(string[])"/>-><see cref="OnRun(string[])"/>-><see cref="OnStartup(StartupEventArgs)"/>-><see cref="OnStartupNextInstance(StartupNextInstanceEventArgs)"/>
    /// Do not mistake with Visual Basic's execute order.
    /// </remarks>
    public abstract class ApplicationBase
    {
        const int MAX_SUPPORTED_ARGUMENT_LENGTH = 32767;

        private bool _isRunning;
        private string _instanceID;

        /// <summary>
        /// Get a boolean determine whether this instance is in single-instance model
        /// </summary>
        protected bool IsSingleInstance => !string.IsNullOrEmpty(this._instanceID);

        /// <summary>
        /// Get the unique instance ID of this application instance
        /// </summary>
        public string SingleInstanceID => this._instanceID;

        /// <summary>
        /// Set the unique instance ID for this application instance. Must be called before <see cref="Run(string[])"/>.
        /// </summary>
        /// <param name="instanceID">The unique ID. Set to null to disable single-instance model</param>
        protected void SetSingleInstanceID(string instanceID)
        {
            this.EnsureApplicationIsNotRunning();
            this._instanceID = instanceID;
        }

        private string GetArgsWriterID(Guid guid)
        {
            return this._instanceID + "-arg-reader" + guid.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationBase"/> class with <see cref="ApplicationBase.IsSingleInstance"/> is false.
        /// </summary>
        protected ApplicationBase() : this(false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationBase"/> class.
        /// </summary>
        /// <param name="isSingleInstance">Determine whether the application is in single-instance model.</param>
        /// <exception cref="InvalidOperationException">
        /// Happens when <paramref name="isSingleInstance"/> is true and the application cannot generate unique ID from the calling assembly.
        /// </exception>
        protected ApplicationBase(bool isSingleInstance) : this(isSingleInstance, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationBase"/> class in single-instance model with given instanceID.
        /// </summary>
        /// <param name="instanceID">The unique ID for the application to check for</param>
        protected ApplicationBase(string instanceID) : this(true, instanceID) { }

        /// <summary>
        /// Nope. Don't even use System.Reflection to invoke this method, please.
        /// </summary>
        /// <param name="isSingleInstance"></param>
        /// <param name="instanceID"></param>
        private ApplicationBase(bool isSingleInstance, string instanceID)
        {
            if (isSingleInstance)
            {
                if (string.IsNullOrEmpty(instanceID))
                {
                    instanceID = this.GetApplicationID();
                }
                else if (instanceID.Length > 260)
                {
                    throw new PathTooLongException("The given instance ID is longer than 260 characters");
                }
                this.SetSingleInstanceID(instanceID);
            }
            else
            {
                this.SetSingleInstanceID(null);
            }
        }

        /// <summary>
        /// Sets up and starts the application model with command-line arguments from <see cref="Environment.GetCommandLineArgs"/>.
        /// </summary>
        public void Run()
        {
            List<string> tmp = new List<string>(Environment.GetCommandLineArgs());
            tmp.RemoveAt(0);

            string[] args = tmp.ToArray();
            tmp = null;
            this.OnRun(args);
            args = null;
        }

        /// <summary>
        /// Sets up and starts the application model with given command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments</param>
        public void Run(string[] args)
        {
            this.OnRun(args);
        }

        /// <summary>
        /// When overridden in a derived class, allows for code to run when the application starts.
        /// </summary>
        /// <param name="eventArgs">Contains the command-line arguments of the application</param>
        protected virtual void OnStartup(StartupEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in a derived class, allows for code to run when a subsequent instance of a single-instance application starts. This method will never be invoked if the application is not in single-instance model.
        /// </summary>
        /// <param name="eventArgs">Contains the command-line arguments of the subsequent application instance</param>
        protected virtual void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides the starting point for when the main application is ready to start running, after the initialization is done.
        /// </summary>
        /// <param name="args"></param>
        private void OnRun(string[] args)
        {
            if (this._isRunning) return;
            this._isRunning = true;

            if (this.IsSingleInstance)
            {
                this.CreateSingleInstanceModel(args);
            }
            else
            {
                this.OnStartup(new StartupEventArgs(args));
            }
        }

        private void CreateSingleInstanceModel(string[] args)
        {
            int maxWritingLength = (MAX_SUPPORTED_ARGUMENT_LENGTH + 8 + 36) * 2;
            using (Mutex mutex = new Mutex(true, Path.Combine("Global", this._instanceID), out var isNewInstanceJustForMeHuh))
            using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, this._instanceID))
            {
                RegisteredWaitHandle waiter = null;
                try
                {
                    if (isNewInstanceJustForMeHuh)
                    {
                        using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(this._instanceID + "-args", maxWritingLength, MemoryMappedFileAccess.ReadWrite))
                        {
                            waiter = ThreadPool.RegisterWaitForSingleObject(waitHandle, new WaitOrTimerCallback((sender, signal) =>
                            {
                                using (BinaryReader br = new BinaryReader(mmf.CreateViewStream(0, maxWritingLength), System.Text.Encoding.Unicode))
                                {
                                    int guidIDLength = br.ReadInt32();
                                    if (guidIDLength != 0)
                                    {
                                        Guid argWaiterGUID = new Guid(br.ReadBytes(guidIDLength));

                                        int arrayCount = br.ReadInt32();
                                        string[] theArgs = new string[arrayCount];
                                        for (int i = 0; i < arrayCount; i++)
                                        {
                                            theArgs[i] = br.ReadString();
                                        }

                                        try
                                        {
                                            this.OnStartupNextInstance(new StartupNextInstanceEventArgs(theArgs));
                                        }
                                        catch
                                        {
                                            if (System.Diagnostics.Debugger.IsAttached)
                                            {
                                                throw;
                                            }
                                        }
                                        finally
                                        {
#if NETSTANDARD2_0
                                            if (EventWaitHandle.TryOpenExisting(this._instanceID + "-argreader" + argWaiterGUID.ToString(), out EventWaitHandle argWaiter))
                                            {
                                                using (argWaiter)
                                                {
                                                    argWaiter.Set();
                                                }
                                            }
#else
                                            try
                                            {
                                                using (EventWaitHandle argWaiter = EventWaitHandle.OpenExisting(this._instanceID + "-argreader" + argWaiterGUID.ToString()))
                                                {
                                                    argWaiter.Set();
                                                }
                                            }
                                            catch { }
#endif
                                        }
                                    }
                                    else
                                    {
                                        // Throw error or should be silently failed?
                                        try
                                        {
                                            this.OnStartupNextInstance(new StartupNextInstanceEventArgs(new Exception("Failed to fetch args")));
                                        }
                                        catch
                                        {
                                            if (System.Diagnostics.Debugger.IsAttached)
                                            {
                                                throw;
                                            }
                                        }
                                    }
                                }
                                //this._instanceID + "-argreader" + readerID
                                // this.OnStartupNextInstance(e);
                            }), null, -1, false);
                            this.OnStartup(new StartupEventArgs(args));
                        }
                    }
                    else
                    {
                        Guid readerID = Guid.NewGuid();
                        using (Mutex writerMutex = new Mutex(true, Path.Combine("Global", this._instanceID + "-argwriter"), out var isNewWriterMutex))
                        using (EventWaitHandle argWaiter = new EventWaitHandle(false, EventResetMode.ManualReset, this._instanceID + "-argreader" + readerID.ToString(), out var newInstanceForArgs))
                        {
                            if (!isNewWriterMutex)
                            {
                                while (!writerMutex.WaitOne()) { }
                            }
                            if (newInstanceForArgs)
                            {
                                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(this._instanceID + "-args", MemoryMappedFileRights.Write))
                                using (var accessor = mmf.CreateViewAccessor(0, maxWritingLength))
                                {
                                    try
                                    {
                                        byte[] buffer = new byte[maxWritingLength];
                                        MemoryStream memStream = new MemoryStream(buffer, true);
                                        memStream.Position = 0;
                                        BinaryWriter bw = new BinaryWriter(memStream, System.Text.Encoding.Unicode);

                                        var guidBuffer = readerID.ToByteArray();

                                        bw.Write(guidBuffer.Length);
                                        bw.Write(guidBuffer);

                                        bw.Write(args.Length);
                                        for (int i = 0; i < args.Length; i++)
                                        {
                                            bw.Write(args[i]);
                                        }

                                        if (memStream.Position <= maxWritingLength)
                                        {
                                            for (int i = 0; i < buffer.Length; i++)
                                            {
                                                accessor.Write(i, buffer[i]);
                                            }

                                            waitHandle.Set();
                                            // wait until the reader finished get the args
                                            argWaiter.WaitOne();
                                        }
                                        else
                                        {
                                            accessor.Write(0, 0);
                                            waitHandle.Set();
                                        }
                                    }
                                    catch
                                    {
                                        accessor.Write(0, 0);
                                        waitHandle.Set();
                                    }
                                }
                            }
                            writerMutex.ReleaseMutex();
                        }
                    }
                }
                finally
                {
                    if (waiter != null)
                    {
                        waiter.Unregister(waitHandle);
                    }

                    // mutex.ReleaseMutex();
                }
            }
        }

        private string GetApplicationID()
        {
#if NETSTANDARD1_5 || NETSTANDARD1_6 || NETSTANDARD2_0
            var entryAssembly = Assembly.GetCallingAssembly();
            var attribute = entryAssembly.GetCustomAttributes(typeof(GuidAttribute)).FirstOrDefault() as GuidAttribute;
            if (attribute != null)
                return attribute.Value;
            else
                throw new InvalidOperationException("Cannot generate unique ID from assembly GUID");
#else
            Assembly entryAssembly = Assembly.GetCallingAssembly();
            System.Security.PermissionSet permissionSet = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
            permissionSet.AddPermission(new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted));
            permissionSet.AddPermission(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode));
            permissionSet.Assert();
            Guid typeLibGuidForAssembly = Marshal.GetTypeLibGuidForAssembly(entryAssembly);
            string[] array = entryAssembly.GetName().Version.ToString().Split(new char[] { '.' });
            System.Security.PermissionSet.RevertAssert();
            return typeLibGuidForAssembly.ToString() + array[0] + "." + array[1];
#endif
        }

        private void EnsureApplicationIsNotRunning()
        {
            if (this._isRunning)
                throw new InvalidOperationException("Cannot change property while the application is running.");
        }
    }
}
