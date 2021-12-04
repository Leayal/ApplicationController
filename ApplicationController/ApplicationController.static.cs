using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Threading;

namespace Leayal.ApplicationController
{
    partial class ApplicationController
    {
        static int StreamDrain(System.IO.Stream stream, byte[] buffer, int offset, int count)
        {
            int left = count;
            var read = stream.Read(buffer, offset, left);
            if (read == count) return count;
            while (read > 0)
            {
                offset += read;
                left -= read;
                read = stream.Read(buffer, offset, left);
            }
            return count - left;
        }

        private static string GenerateAutoIdentifier()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                string baseToGenerate;
                if (proc.MainModule != null && proc.MainModule.FileName != null)
                {
                    baseToGenerate = proc.MainModule.FileName;
                }
                else
                {
                    var entry = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
                    baseToGenerate = entry.Location;
                    if (string.IsNullOrEmpty(baseToGenerate))
                    {
                        baseToGenerate = proc.ProcessName;
                    }
                }
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var hash = md5.ComputeHash(Encoding.Unicode.GetBytes(baseToGenerate));
#if NET5_0_OR_GREATER
                    return Convert.ToHexString(hash);
#else
                    StringBuilder hex = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash)
                        hex.AppendFormat("{0:x2}", b);
                    return hex.ToString();
#endif
                }
            }
        }

#if NET5_0_OR_GREATER
#else
        private class TaskCompletionSourceWithoutArgs : TaskCompletionSource<bool>
        {
            public void Yeet() => this.SetResult(true);
        }
#endif

        private static async Task WaitForConnectionAsyncThatActuallyReturnWhenCancelled(NamedPipeServerStream pipe, CancellationToken token)
        {
#if NET5_0_OR_GREATER
            var tTermination = new TaskCompletionSource();
            token.Register(tTermination.SetResult);
#else
            var tTermination = new TaskCompletionSourceWithoutArgs();
            token.Register(tTermination.Yeet);
#endif
            var tCancel = tTermination.Task;
            var t_pipe = pipe.WaitForConnectionAsync(token);
            var t = await Task.WhenAny(t_pipe, tCancel);
            if (tCancel == t)
            {
                if (t_pipe.IsCompleted)
                {
                    t_pipe.Dispose();
                }
            }
            else
            {
                tTermination.SetCanceled();
                t_pipe.Dispose();
                if (tCancel.IsCompleted)
                {
                    tCancel.Dispose();
                }
            }
        }
    }
}
