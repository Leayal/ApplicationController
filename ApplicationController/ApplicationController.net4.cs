#if NETFRAMEWORK || NETCOREAPP2_1
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.ApplicationController
{
    partial class ApplicationController
    {
        static ArraySegment<byte> CraftPacket(int processId, string[] args)
        {
            using (var memStream = new MemoryStream())
            using (var encoder = new BinaryWriter(memStream, Encoding.UTF8, true))
            {
                encoder.Write(processId);
                encoder.Write(args.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    encoder.Write(args[i]);
                }
                encoder.Flush();

                if (memStream.TryGetBuffer(out var seg))
                {
                    return seg;
                }
                else
                {
                    return new ArraySegment<byte>(memStream.ToArray());
                }
            }
        }

        static NextInstanceData ParsePacket(Stream dataStream, int length)
        {
            var buffer = new byte[length];

            var read = StreamDrain(dataStream, buffer, 0, length);
            if (read == length)
            {
                using (var mem = new MemoryStream(buffer, false))
                using (var decoder = new BinaryReader(mem, Encoding.UTF8, false))
                {
                    var processId = decoder.ReadInt32();
                    var argCount = decoder.ReadInt32();
                    var args = new string[argCount];
                    for (int i = 0; i < argCount; i++)
                    {
                        args[i] = decoder.ReadString();
                    }
                    return new NextInstanceData(processId, args);
                }
            }

            return null;
        }
    }
}
#endif