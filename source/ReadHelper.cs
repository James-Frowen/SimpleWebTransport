#define SIMPLE_WEB_INFO_LOG
using System.IO;

namespace Mirror.SimpleWeb
{
    public static class ReadHelper
    {
        public static bool SafeRead(Stream stream, byte[] outBuffer, int outOffset, int length)
        {
            try
            {
                int recieved = stream.Read(outBuffer, outOffset, length);

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

        public static int? SafeReadTillMatch(Stream stream, byte[] outBuffer, int outOffset, byte[] endOfHeader)
        {
            try
            {
                int read = 0;
                int endIndex = 0;
                int endLength = endOfHeader.Length;
                while (true)
                {
                    int next = stream.ReadByte();
                    if (next == -1) // closed
                        return null;

                    outBuffer[outOffset + read] = (byte)next;
                    read++;

                    // if n is match, check n+1 next
                    if (endOfHeader[endIndex] == next)
                    {
                        endIndex++;
                        // when all is match return with read length
                        if (endIndex >= endLength)
                        {
                            return read;
                        }
                    }
                    // if n not match reset to 0
                    else
                    {
                        endIndex = 0;
                    }
                }
            }
            catch (IOException e)
            {
                Log.Info($"SafeRead IOException\n{e.Message}", false);
                return null;
            }
        }
    }
}
