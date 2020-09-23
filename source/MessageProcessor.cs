#define SIMPLE_WEB_INFO_LOG
using System.IO;

namespace Mirror.SimpleWeb
{
    public static class MessageProcessor
    {
        public struct Result
        {
            public int opcode;

            public int maskOffset;
            public int msgLength;

            /// <summary>
            /// how much more data there is to read
            /// </summary>
            public int readLength => msgLength + maskOffset;

            /// <summary>
            /// when message starts
            /// </summary>
            public int msgOffset => maskOffset + 4;
        }


        public static Result ProcessHeader(byte[] buffer, int maxLength)
        {
            bool finished = (buffer[0] & 0b1000_0000) != 0; // has full message been sent
            bool hasMask = (buffer[1] & 0b1000_0000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = buffer[0] & 0b0000_1111; // expecting 1 - text message
            byte lenByte = (byte)(buffer[1] & 0b0111_1111); // first length byte

            ThrowIfNotFinished(finished);
            ThrowIfNoMask(hasMask);
            ThrowIfBadOpCode(opcode);

            // offset is 2 or 4
            (int msglen, int maskOffset) = GetMessageLength(buffer, 0, lenByte);

            ThrowIfLengthZero(msglen);
            ThrowIfMsgLengthTooLong(msglen, maxLength);

            return new Result
            {
                opcode = opcode,
                maskOffset = maskOffset,
                msgLength = msglen,
            };
        }


        public static void DecodeMessage(byte[] buffer, int maskOffset, int messageLength)
        {
            int offset = maskOffset + 4;

            for (int i = 0; i < messageLength; i++)
            {
                byte maskByte = buffer[maskOffset + i % 4];
                buffer[offset + i] = (byte)(buffer[offset + i] ^ maskByte);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns>bytes processed</returns>
        /// <exception cref="InvalidDataException"></exception>
        [System.Obsolete("Old", true)]
        public static Result ProcessMessage(byte[] buffer, int offset, int length)
        {
            bool finished = (buffer[offset + 0] & 0b1000_0000) != 0; // has full message been sent
            bool hasMask = (buffer[offset + 1] & 0b1000_0000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = buffer[offset + 0] & 0b0000_1111; // expecting 1 - text message
            byte lenByte = (byte)(buffer[offset + 1] & 0b0111_1111); // first length byte

            ThrowIfNotFinished(finished);
            ThrowIfNoMask(hasMask);
            ThrowIfBadOpCode(opcode);

            int msglen;
            (msglen, offset) = GetMessageLength(buffer, offset, lenByte);

            ThrowIfLengthZero(msglen);
            ThrowIfMsgLengthTooLong(msglen, length);

            int maskOffset = offset;
            offset += 4;

            for (int i = 0; i < msglen; i++)
            {
                byte maskByte = buffer[maskOffset + i % 4];
                buffer[offset + i] = (byte)(buffer[offset + i] ^ maskByte);
            }


            return new Result
            {
                opcode = opcode,
                maskOffset = offset,
                msgLength = msglen,
            };
        }

        /// <exception cref="InvalidDataException"></exception>
        static (int length, int maskOffset) GetMessageLength(byte[] buffer, int offset, byte lenByte)
        {
            if (lenByte == 126)
            {
                ushort value = 0;
                value |= (ushort)(buffer[offset + 2] << 8);
                value |= buffer[offset + 3];

                return (value, offset + 4);
            }
            else if (lenByte == 127)
            {
                throw new InvalidDataException("Max length is longer than allowed in a single message");
            }
            else // is less than 126
            {
                return (lenByte, offset + 2);
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void ThrowIfNotFinished(bool finished)
        {
            if (!finished)
            {
                // TODO check if we need to deal with this
                throw new InvalidDataException("Full message should have been sent, if the full message wasn't sent it wasn't sent from this trasnport");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void ThrowIfNoMask(bool hasMask)
        {
            if (!hasMask)
            {
                throw new InvalidDataException("Message from client should have mask set to true");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void ThrowIfBadOpCode(int opcode)
        {
            // 2 = binary
            // 8 = close
            if (opcode != 2 && opcode != 8)
            {
                throw new InvalidDataException("Expected opcode to be binary or close");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void ThrowIfLengthZero(int msglen)
        {
            if (msglen == 0)
            {
                throw new InvalidDataException("Message length was zero");
            }
        }

        /// <summary>
        /// need to check this so that data from previous buffer isnt used
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        static void ThrowIfMsgLengthTooLong(int msglen, int maxLength)
        {
            if (msglen > maxLength)
            {
                throw new InvalidDataException("Message length is greater than max length");
            }
        }
    }
}
