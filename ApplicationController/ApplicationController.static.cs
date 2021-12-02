using System;
using System.Text;
using System.Diagnostics;

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
    }
}
