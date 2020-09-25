#define SIMPLE_WEB_INFO_LOG
using System;
using System.IO;

namespace Mirror.SimpleWeb
{
    public static class ReadHelper
    {
        public static bool SafeRead(Stream stream, byte[] buffer, int offset, int length)
        {
            try
            {
                int recieved = stream.Read(buffer, offset, length);

                if (recieved == -1)
                {
                    return false;
                }

                return true;
            }
            catch (IOException e)
            {
                Log.Info($"SafeRead IOException\n{e.Message}", false);
                return false;
            }
        }

        public static int SafeReadToEnd(Stream stream, byte[] buffer, int offset)
        {
            int read = 0;
            while (true)
            {
                int next = stream.ReadByte();
                if (next == -1)
                    break;

                Console.Write((char)next);
                buffer[offset + read] = (byte)next;
                read++;
            }
            Console.Write(read);

            return read;
        }
    }
}
