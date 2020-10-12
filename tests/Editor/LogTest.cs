// define this to make sure log level works
#define SIMPLEWEB_LOG_ENABLED
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class LogTest
    {
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
        public void DumpMessageTest(bool hasLabel, Log.Levels levels)
        {
            Log.level = levels;
            const string Label = "fun label";

            byte[] data = new byte[5] { 10, 11, 12, 13, 14 };
            string expected = "0B-0C-0D";

            if (levels >= Log.Levels.verbose)
            {
                if (hasLabel)
                    LogAssert.Expect(UnityEngine.LogType.Log, $"VERBOSE: <color=blue>{Label}: {expected}</color>");
                else
                    LogAssert.Expect(UnityEngine.LogType.Log, $"VERBOSE: <color=blue>{expected}</color>");
            }

            if (hasLabel)
                Log.DumpBuffer(Label, data, 1, 3);
            else
                Log.DumpBuffer(data, 1, 3);

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
    }
}
