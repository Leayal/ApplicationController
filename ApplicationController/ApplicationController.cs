using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.ApplicationController
{
    /// <summary>Provides controller to interact with the main frame of the whole single-instance application.</summary>
    /// <remarks>This class is single-instance-oriented.</remarks>
    public abstract partial class ApplicationController : IDisposable
    {
        #region | Private Fields |
#if !NETFRAMEWORK
        private static readonly bool isOnWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#endif
        private readonly Mutex mutex;

#if NETCOREAPP3_1_OR_GREATER
        private NamedPipeServerStream? pipeServerStream;
        private readonly CancellationTokenSource? cancellationTokenSource;
        private readonly Action<int, string[]>? nextInstanceInvoker;
#else
        private NamedPipeServerStream pipeServerStream;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Action<int, string[]> nextInstanceInvoker;
#endif
        #endregion

        #region | Public Fields |
        /// <summary>The boolean determines whether the current application instance is the first instance or not.</summary>
        public readonly bool IsFirstInstance;

        /// <summary>The unique name of the application instance.</summary>
        public readonly string UniqueIdentifier;
#endregion

        #region | Constructors |
        /// <summary>Constructor for all the derived classes to specify definite identifier</summary>
        /// <param name="identifierName">The unique name to be used for Mutexes</param>
        protected ApplicationController(string identifierName)
        {
            this.UniqueIdentifier = identifierName;
            this.mutex = new Mutex(true, identifierName + "-mutex", out this.IsFirstInstance);
            if (this.IsFirstInstance)
            {
                this.nextInstanceInvoker = this.OnStartupNextInstance;
                this.cancellationTokenSource = new CancellationTokenSource();
                this.pipeServerStream = this.CreateNewServerPipe();
            }
            else
            {
                this.nextInstanceInvoker = null;
                this.cancellationTokenSource = null;
                this.pipeServerStream = null;
            }
        }

        /// <summary>Default construct to quickly setup a single-instance application.</summary>
        /// <remarks>This shouldn't be used as the unique name identifier is not definite.</remarks>
        public ApplicationController() : this(GenerateAutoIdentifier()) { }
#endregion

        #region | Abstract Methods |
        /// <summary>When overriden, provides the startup logic when the instance is first launched.</summary>
        /// <param name="args">The process command-line when it's launched for the first time (does not include process's image path). Will never be null, but possibly be empty.</param>
        protected abstract void OnStartupFirstInstance(string[] args);

        /// <summary>When overriden, provides the startup logic when the instance is launched after the first time.</summary>
        /// <param name="processId">The process ID of the subsequent instance.</param>
        /// <param name="args">The process command-line of the subsequent instances (does not include process's image path). Will never be null, but possibly be empty.</param>
        /// <remarks>
        /// <para>This method will be called in the first instance's process.</para>
        /// <para>This method may be called on a different thread instead of the application main thread.</para>
        /// </remarks>
        protected abstract void OnStartupNextInstance(int processId, string[] args);
        #endregion

        #region | Public Methods |
        /// <summary>Initialize and run the application with empty argument.</summary>
        public void Run() => this.Run(Array.Empty<string>());

        /// <summary>Initialize and run the application</summary>
        /// <param name="args">The passing arguments to the application. Can't be null but can be empty.</param>
        public void Run(string[] args)
        {
            try
            {
                if (this.IsFirstInstance)
                {
                    Task t;
                    if (this.pipeServerStream != null)
                    {
                        t = Task.Factory.StartNew(this.WaitForNextInstance, TaskCreationOptions.LongRunning).Unwrap();
                    }
                    else
                    {
                        t = Task.CompletedTask;
                    }
                    this.OnStartupFirstInstance(args ?? Array.Empty<string>());
                    this.cancellationTokenSource?.Cancel();

                    // Dirty one
                    t.GetAwaiter().GetResult();
                }
                else
                {
                    int procId;
                    using (var proc = System.Diagnostics.Process.GetCurrentProcess())
                    {
                        procId = proc.Id;
                    }

                    var seg = CraftPacket(procId, args);
                    if (seg.Array != null)
                    {
                        using (var clientPipe = this.CreateNewClientPipe())
                        {
                            clientPipe.Connect(5000);

                            var lenPacket = BitConverter.GetBytes(seg.Count);
                            clientPipe.Write(lenPacket, 0, lenPacket.Length);
                            clientPipe.Write(seg.Array, seg.Offset, seg.Count);

                            if (clientPipe.IsConnected)
                            {
                                try
                                {
#if NETFRAMEWORK
                                    clientPipe.WaitForPipeDrain();
#else
                                if (isOnWindows)
                                {
#pragma warning disable CA1416 // Validate platform compatibility
                                    clientPipe.WaitForPipeDrain();
#pragma warning restore CA1416 // Validate platform compatibility
                                }
                                else
                                {
                                    clientPipe.ReadByte();
                                }
#endif
                                }
                                catch (ObjectDisposedException) { }
                                catch (IOException) { }
                            }
                        }
                    }
                }
            }
            finally
            {
                this.Dispose();
            }
        }
#endregion

        #region | Private Methods |
#if NETCOREAPP2_1_OR_GREATER
#if NETCOREAPP2_1
        private void InvokeNextInstance(object obj)
#else
        private void InvokeNextInstance(object? obj)
#endif
        {
            if (obj is NextInstanceData data)
            {
                this.nextInstanceInvoker?.Invoke(data.ProcessId, data.Arguments);
            }
        }
#endif

        private NamedPipeServerStream CreateNewServerPipe()
        {
#if NETFRAMEWORK
            return new NamedPipeServerStream(this.UniqueIdentifier + "-pipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None);
#else
            if (isOnWindows)
            {
                return new NamedPipeServerStream(this.UniqueIdentifier + "-pipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None);
            }
            else
            {
                return new NamedPipeServerStream(this.UniqueIdentifier + "-pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None);
            }
#endif
        }

        private NamedPipeClientStream CreateNewClientPipe()
        {
#if NETFRAMEWORK
            return new NamedPipeClientStream(".", this.UniqueIdentifier + "-pipe", PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.Impersonation);
#else
            if (isOnWindows)
            {
                return new NamedPipeClientStream(".", this.UniqueIdentifier + "-pipe", PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            }
            else
            {
                return new NamedPipeClientStream(".", this.UniqueIdentifier + "-pipe", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            }
#endif
        }

        private async Task WaitForNextInstance()
        {
            if (this.pipeServerStream == null || this.cancellationTokenSource == null || this.nextInstanceInvoker == null) return;
            var token = this.cancellationTokenSource.Token;
            while (true)
            {
                await this.pipeServerStream.WaitForConnectionAsync(token);
                if (token.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    var oldPipe = this.pipeServerStream;
                    var len = new byte[4];
                    var read = StreamDrain(oldPipe, len, 0, len.Length);
                    if (read == 4)
                    {
                        var msgLength = BitConverter.ToInt32(len, 0);
                        var data = ParsePacket(this.pipeServerStream, msgLength);
                        if (data != null)
                        {
#if NETFRAMEWORK
                            this.nextInstanceInvoker.BeginInvoke(data.ProcessId, data.Arguments, this.nextInstanceInvoker.EndInvoke, null);
#else
                            await Task.Factory.StartNew(this.InvokeNextInstance, data);
                            if (!isOnWindows)
                            {
                                oldPipe.WriteByte(1);
                                oldPipe.Flush();
                            }
#endif
                        }
                    }
                    oldPipe.Disconnect();
                    oldPipe.Dispose();
                    this.pipeServerStream = this.CreateNewServerPipe();
                }
            }
        }
        #endregion

        #region | Dispose Methods |
        /// <summary>Clean up all resources and handles allocated by this instance.</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>When override, provides the cleanup of all resources allocated.</summary>
        /// <param name="disposing">The boolean determines whether the dispose method is called manually or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.IsFirstInstance)
            {
                this.mutex.ReleaseMutex();
                this.pipeServerStream?.Dispose();
                this.cancellationTokenSource?.Dispose();
            }
            this.mutex.Dispose();
        }

        /// <summary>Deconstructor</summary>
        ~ApplicationController()
        {
            this.Dispose(false);
        }
#endregion
    }
}
