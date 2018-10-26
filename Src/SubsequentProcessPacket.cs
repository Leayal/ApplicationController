using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Leayal.ApplicationController
{
    class SubsequentProcessPacket
    {
        // internal const int PacketFixedSize = 29;

        public static SubsequentProcessPacket FromStream(Stream stream)
        {
            SubsequentProcessPacket result = new SubsequentProcessPacket();
            var buffer = result.BuildPacket();
            if (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                int sizeofInt = sizeof(int),
                    sizeofBool = sizeof(bool),
                    sizeofLong = sizeof(long);
                
                byte[] guiddata = new byte[buffer.Length - sizeofInt - sizeofBool - sizeofLong];
                Buffer.BlockCopy(buffer, sizeofInt + sizeofBool, guiddata, 0, guiddata.Length);

                result.ArgumentCount = BitConverter.ToInt32(buffer, 0);
                result.IsMemorySharing = BitConverter.ToBoolean(buffer, sizeofInt);
                result.SharedID = new Guid(guiddata);
                result.DataLength = BitConverter.ToInt64(buffer, sizeofInt + sizeofBool + guiddata.Length);

                return result;
            }
            else
            {
                return null;
            }
        }

        // Internally use so no need to make it read-only properties or anything.
        public bool IsMemorySharing;
        public Guid SharedID;
        public int ArgumentCount;
        public long DataLength;

        public SubsequentProcessPacket() : this(0, true, Guid.NewGuid(), 0) { }

        public SubsequentProcessPacket(int argCount, bool shareInMemory, Guid sharingID, long dataLength)
        {
            this.ArgumentCount = argCount;
            this.SharedID = sharingID;
            this.IsMemorySharing = shareInMemory;
            this.DataLength = dataLength;
        }

        public int BuildPacket(byte[] buffer, int offset)
        {
            int sizeofInt = sizeof(int),
               sizeofBool = sizeof(bool),
               sizeofLong = sizeof(long);
            byte[] sharediddata = SharedID.ToByteArray();
            int packetsize = sizeofInt + sizeofBool + sharediddata.Length + sizeofLong;

            if (buffer.Length < packetsize)
            {
                throw new ArgumentException($"Buffer size must have at least {packetsize}-byte");
            }

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                Marshal.WriteInt32(handle.AddrOfPinnedObject(), offset, this.ArgumentCount);
                Marshal.WriteByte(handle.AddrOfPinnedObject(), offset + sizeofInt, this.IsMemorySharing ? (byte)1 : (byte)0);
                Buffer.BlockCopy(sharediddata, 0, buffer, offset + sizeofInt + sizeofBool, sharediddata.Length);
                Marshal.WriteInt64(handle.AddrOfPinnedObject(), offset + sizeofInt + sizeofBool + sharediddata.Length, this.DataLength);
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return packetsize;
        }

        public byte[] BuildPacket()
        {
            int sizeofInt = sizeof(int),
                sizeofBool = sizeof(bool),
                sizeofLong = sizeof(long);
            byte[] sharediddata = SharedID.ToByteArray();
            byte[] result = new byte[sizeofInt + sizeofBool + sizeofLong + sharediddata.Length];

            var handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            try
            {
                Marshal.WriteInt32(handle.AddrOfPinnedObject(), this.ArgumentCount);
                Marshal.WriteByte(handle.AddrOfPinnedObject(), sizeofInt, this.IsMemorySharing ? (byte)1 : (byte)0);
                Buffer.BlockCopy(sharediddata, 0, result, sizeofInt + sizeofBool, sharediddata.Length);
                Marshal.WriteInt64(handle.AddrOfPinnedObject(), sizeofInt + sizeofBool + sharediddata.Length, this.DataLength);
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return result;
        }
    }
}
