using System.IO;
using NUnit.Framework;

namespace JamesFrowen.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class MessageProcessorTests
    {
        // TODO Needs updating for new Message Processor methods
        private byte[] CreateMessage(bool finished = true, bool hasMask = true, int opcode = 2, int length = 10)
        {
            byte[] buffer = new byte[20];

            buffer[0] |= (byte)(finished ? 0b1000_0000 : 0);
            buffer[1] |= (byte)(hasMask ? 0b1000_0000 : 0);

            buffer[0] |= (byte)(opcode & 0b0000_1111);

            if (length < 126)
            {
                buffer[1] |= (byte)(length & 0b0111_1111);
            }
            else if (126 <= length && length <= ushort.MaxValue)
            {
                buffer[1] |= 126 & 0b0111_1111;
                buffer[2] = (byte)(length >> 8);
                buffer[3] = (byte)length;
            }
            else if (ushort.MaxValue < length && (ulong)length <= ulong.MaxValue)
            {
                buffer[1] |= 127 & 0b0111_1111;

                buffer[6] = (byte)(length >> 24);
                buffer[7] = (byte)(length >> 16);
                buffer[8] = (byte)(length >> 8);
                buffer[9] = (byte)(length >> 0);
            }

            return buffer;
        }

        [Test]
        [TestCase(2, 10)]
        [TestCase(2, 100)]
        [TestCase(2, 125)]
        [TestCase(2, 126)]
        [TestCase(2, 127)]
        [TestCase(2, 128)]
        [TestCase(2, 1000)]
        [TestCase(8, 10)]
        [TestCase(8, 100)]
        [TestCase(8, 125)]
        [TestCase(8, 126)]
        [TestCase(8, 127)]
        [TestCase(8, 128)]
        [TestCase(8, 1000)]
        [Ignore("Broken")]
        public void HasCorrectValues(int opCode, int length)
        {
            //byte[] buffer = CreateMessage(opcode: opCode, length: length);

            //MessageProcessor.Result result = MessageProcessor.ValidateHeader(buffer, length, true);

            //Assert.That(result.opcode, Is.EqualTo(opCode));
            //Assert.That(result.offset, Is.EqualTo(length < 126 ? 2 : 4));
            //Assert.That(result.msgLength, Is.EqualTo(length));
        }

        [Test]
        public void ThrowsWhenNotFinished()
        {
            byte[] buffer = CreateMessage(finished: false);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);
            });

            Assert.That(expection.Message, Is.EqualTo("Full message should have been sent, if the full message wasn't sent it wasn't sent from this trasnport"));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ThrowsWhenMaskIsWrong(bool mask)
        {
            byte[] buffer = CreateMessage(hasMask: !mask);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, expectMask: mask);
            });

            Assert.That(expection.Message, Is.EqualTo($"Message expected mask to be {mask} but was {!mask}"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        public void ThrowsForBadOpCode(int opcode)
        {
            byte[] buffer = CreateMessage(opcode: opcode);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);
            });

            Assert.That(expection.Message, Is.EqualTo("Expected opcode to be binary or close"));
        }

        [Test]
        public void ThrowsWhenLengthIsZero()
        {
            byte[] buffer = CreateMessage(length: 0);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);
            });

            Assert.That(expection.Message, Is.EqualTo("Message length was zero"));
        }

        [Test]
        public void ThrowsWhenLengthWasGreaterThanBuffer()
        {
            byte[] buffer = CreateMessage(length: 10 * 1024 + 1);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);
            });

            Assert.That(expection.Message, Is.EqualTo("Message length is greater than max length"));
        }

        [Test]
        [Ignore("Broken")]
        public void MessageAtMaxLengthIsOk()
        {
            //byte[] buffer = CreateMessage(length: 10 * 1024);

            //MessageProcessor.Result result = MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);

            //Assert.That(result.opcode, Is.EqualTo(2));
            //Assert.That(result.offset, Is.EqualTo(4));
            //Assert.That(result.msgLength, Is.EqualTo(10 * 1024));
        }

        [Test]
        public void ThrowsWhenMessageIsCreaterThanUshortMax()
        {
            byte[] buffer = CreateMessage(length: ushort.MaxValue * 1000);

            InvalidDataException expection = Assert.Throws<InvalidDataException>(() =>
            {
                MessageProcessor.ValidateHeader(buffer, 10 * 1024, true);
            });

            Assert.That(expection.Message, Is.EqualTo("Message length is greater than max length"));
        }
    }
}
