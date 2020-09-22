#define SIMPLE_WEB_INFO_LOG
using System.IO;
using System.Net.Sockets;

namespace Mirror.SimpleWeb
{
    public static class ReadHelper
    {
        public static bool SafeRead(NetworkStream stream, byte[] buffer, int offset, int length)
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
    }
}
