#if NETCOREAPP3_1_OR_GREATER
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Leayal.ApplicationController
{
    partial class ApplicationController
    {
        static ArraySegment<byte> CraftPacket(int processId, string[] args)
        {
            using (var memStream = new MemoryStream())
            using (var encoder = new Utf8JsonWriter(memStream, new JsonWriterOptions() { Indented = false, SkipValidation = false }))
            {
                encoder.WriteStartObject();
                encoder.WriteNumber("processid", processId);
                if (args != null && args.Length != 0)
                {
                    encoder.WriteStartArray("args");
                    for (int i = 0; i < args.Length; i++)
                    {
                        encoder.WriteStringValue(args[i]);
                    }
                    encoder.WriteEndArray();
                }
                encoder.WriteEndObject();
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

        static NextInstanceData? ParsePacket(Stream dataStream, int length)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
            try
            {
                var read = StreamDrain(dataStream, buffer, 0, length);
                if (read == length)
                {
                    var bufferView = new ReadOnlyMemory<byte>(buffer, 0, length);
                    var aaaa = Encoding.UTF8.GetString(bufferView.Span);
                    using (var doc = JsonDocument.Parse(bufferView, new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip }))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("processid", out var prop_procId) && prop_procId.ValueKind == JsonValueKind.Number)
                        {
                            var nextInstanceProcId = prop_procId.GetInt32();
                            string[] args = Array.Empty<string>();
                            if (root.TryGetProperty("args", out var prop_args) && prop_args.ValueKind == JsonValueKind.Array)
                            {
                                var arrLength = prop_args.GetArrayLength();
                                if (arrLength != 0)
                                {
                                    args = new string[arrLength];
                                    int i = 0;
                                    using (var walker = prop_args.EnumerateArray())
                                    {
                                        while (walker.MoveNext())
                                        {
                                            args[i++] = walker.Current.GetString() ?? string.Empty;
                                        }
                                    }
                                }
                            }
                            return new NextInstanceData(nextInstanceProcId, args);
                        }
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer, true);
            }

            return null;
        }
    }
}
#endif