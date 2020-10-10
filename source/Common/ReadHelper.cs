using System;
using System.IO;

namespace Mirror.SimpleWeb
{
    public class ReadHelperException : Exception
    {
        public ReadHelperException(string message) : base(message) { }
    }

    public static class ReadHelper
    {
        /// <exception cref="ReadHelperException"></exception>
        /// <exception cref="IOException"></exception>
        public static void Read(Stream stream, byte[] outBuffer, int outOffset, int length)
        {
            int received = 0;
            try
            {
                received = stream.Read(outBuffer, outOffset, length);
            }
            catch (AggregateException ae)
            {
                // if interupt is called we dont care about Exceptions
                Utils.CheckForInterupt();

                ae.Handle(e =>
                {
                    // rethrow
                    return false;
                });
            }

            if (received == -1)
            {
                throw new ReadHelperException("returned -1");
            }

            if (received == 0)
            {
                throw new ReadHelperException("returned 0");
            }
            if (received != length)
            {
                throw new ReadHelperException("returned not equal to length");
            }
        }

        public enum ReadResult
        {
            Success = 1,
            ReadMinusOne = 2,
            ReadZero = 4,
            ReadLessThanLength = 8,
            Error = 16,
            StreamDisposed = 32,
            Fail = ReadMinusOne | ReadZero | ReadLessThanLength | Error | StreamDisposed

        }

        /// <summary>
        /// Reads and returns results. This should never throw an exception
        /// </summary>
        public static ReadResult SafeRead(Stream stream, byte[] outBuffer, int outOffset, int length)
        {
            try
            {
                int received = stream.Read(outBuffer, outOffset, length);

                if (received == -1)
                {
                    return ReadResult.ReadMinusOne;
                }

                if (received == 0)
                {
                    return ReadResult.ReadZero;
                }
                if (received != length)
                {
                    return ReadResult.ReadLessThanLength;
                }

                return ReadResult.Success;
            }
            catch (AggregateException ae)
            {
                // if interupt is called we dont care about Exceptions
                Utils.CheckForInterupt();

                ReadResult result = ReadResult.Error;

                ae.Handle(e =>
                {
                    if (e is IOException io)
                    {
                        // this is only info as SafeRead is allowed to fail
                        Log.Info($"SafeRead IOException\n{io.Message}", false);
                        return true;
                    }
                    if (e is ObjectDisposedException)
                    {
                        result = ReadResult.StreamDisposed;
                        return true;
                    }

                    return false;
                });

                return result;
            }
            catch (IOException e)
            {
                // if interupt is called we dont care about Exceptions
                Utils.CheckForInterupt();

                // this is only info as SafeRead is allowed to fail
                Log.Info($"SafeRead IOException\n{e.Message}", false);
                return ReadResult.Error;
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
