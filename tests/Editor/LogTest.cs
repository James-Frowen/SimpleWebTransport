// define this to make sure log level works
#define SIMPLEWEB_LOG_ENABLED
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace JamesFrowen.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class LogTest
    {
        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }
        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        static IEnumerable BufferToStringTestCases
        {
            get
            {
                yield return new TestCaseData(new byte[5] { 10, 11, 12, 13, 14 }, null, null).Returns("0A-0B-0C-0D-0E");
                yield return new TestCaseData(new byte[5] { 10, 11, 12, 13, 14 }, 1, 3).Returns("0B-0C-0D");
                yield return new TestCaseData(new byte[5] { 255, 0, 128, 13, 14 }, null, 3).Returns("FF-00-80");
                yield return new TestCaseData(new byte[5] { 255, 0, 128, 13, 14 }, null, 5).Returns("FF-00-80-0D-0E");

                byte[] data = Enumerable.Range(0, 255).Select(x => (byte)x).ToArray();
                // charaters are split by '-'
                // charaters are in hex
                // charaters are padded (eg 01 instead of 1)
                string expected = string.Join("-", data.Select(x => x.ToString("X").PadLeft(2, '0')));
                yield return new TestCaseData(data, null, null).Returns(expected);
            }
        }
        [Test]
        [TestCaseSource(nameof(BufferToStringTestCases))]
        public string BufferToString(byte[] buffer, int? offset, int? length)
        {
            return Log.BufferToString(buffer, offset ?? 0, length);
        }


        const string SomeMessage = "Some Message";

        static IEnumerable TestCases
        {
            get
            {
                for (int i = 0; i < 2; i++)
                {
                    foreach (Log.Levels level in (Log.Levels[])Enum.GetValues(typeof(Log.Levels)))
                    {
                        yield return new object[] { i == 1, level };
                    }
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void DumpMessageTest(bool asArrayBuffer, Log.Levels levels)
        {
            Log.level = levels;
            const string Label = "fun label";

            byte[] data = new byte[5] { 10, 11, 12, 13, 14 };
            string expected = "0B-0C-0D";

            if (levels >= Log.Levels.verbose)
            {
                LogAssert.Expect(UnityEngine.LogType.Log, $"VERBOSE: <color=blue>{Label}: {expected}</color>");
            }

            if (asArrayBuffer)
            {
                var buffer = new ArrayBuffer(null, 10);
                buffer.CopyFrom(data, 1, 3);
                Log.DumpBuffer(Label, buffer);
            }
            else
            {
                Log.DumpBuffer(Label, data, 1, 3);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ExceptionTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            Exception myException = new IOException(SomeMessage);
            // Exception isnt effected by log level
            LogAssert.Expect(UnityEngine.LogType.Error, $"EXCEPTION: <color=red>{nameof(IOException)}</color> Message: {SomeMessage}\n{myException.StackTrace}");
            Log.Exception(myException);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ErrorTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            if (levels >= Log.Levels.error)
            {
                if (showColor)
                    LogAssert.Expect(UnityEngine.LogType.Error, $"ERROR: <color=red>{SomeMessage}</color>");
                else
                    LogAssert.Expect(UnityEngine.LogType.Error, $"ERROR: {SomeMessage}");
            }
            Log.Error(SomeMessage, showColor);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void InfoExceptionTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            Exception myException = new IOException(SomeMessage);

            if (levels >= Log.Levels.info)
            {
                LogAssert.Expect(UnityEngine.LogType.Log, $"INFO_EXCEPTION: <color=blue>{nameof(IOException)}</color> Message: {SomeMessage}");
            }
            Log.InfoException(myException);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void InfoTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            if (levels >= Log.Levels.info)
            {
                if (showColor)
                    LogAssert.Expect(UnityEngine.LogType.Log, $"INFO: <color=blue>{SomeMessage}</color>");
                else
                    LogAssert.Expect(UnityEngine.LogType.Log, $"INFO: {SomeMessage}");
            }
            Log.Info(SomeMessage, showColor);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void VerboseTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            if (levels >= Log.Levels.verbose)
            {
                if (showColor)
                    LogAssert.Expect(UnityEngine.LogType.Log, $"VERBOSE: <color=blue>{SomeMessage}</color>");
                else
                    LogAssert.Expect(UnityEngine.LogType.Log, $"VERBOSE: {SomeMessage}");
            }
            Log.Verbose(SomeMessage, showColor);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void WarnTest(bool showColor, Log.Levels levels)
        {
            Log.level = levels;

            if (levels >= Log.Levels.warn)
            {
                if (showColor)
                    LogAssert.Expect(UnityEngine.LogType.Warning, $"WARN: <color=orange>{SomeMessage}</color>");
                else
                    LogAssert.Expect(UnityEngine.LogType.Warning, $"WARN: {SomeMessage}");
            }
            Log.Warn(SomeMessage, showColor);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
