using System;
using UnityEngine;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Mirror.SimpleWeb
{
    public static class Log
    {
        // used for Conditional
        const string SIMPLEWEB_LOG_ENABLED = nameof(SIMPLEWEB_LOG_ENABLED);
        const string DEBUG = nameof(DEBUG);

        public enum Levels
        {
            none = 0,
            error = 1,
            warn = 2,
            info = 3,
            verbose = 4,
        }

        public static ILogger logger = Debug.unityLogger;
        public static Levels level = Levels.none;

        [Conditional(SIMPLEWEB_LOG_ENABLED)]
        public static void DumpBuffer(byte[] buffer, int offset, int length)
        {
            if (level < Levels.verbose)
                return;

            string text = BitConverter.ToString(buffer, offset, length);
            logger.Log($"VERBOSE: <color=blue>{text}</color>");
        }

        [Conditional(SIMPLEWEB_LOG_ENABLED)]
        public static void DumpBuffer(string label, byte[] buffer, int offset, int length)
        {
            if (level < Levels.verbose)
                return;

            string text = BitConverter.ToString(buffer, offset, length);
            logger.Log($"VERBOSE: <color=blue>{label}: {text}</color>");
        }

        [Conditional(SIMPLEWEB_LOG_ENABLED)]
        public static void Verbose(string msg, bool showColor = true)
        {
            if (level < Levels.verbose)
                return;

            if (showColor)
                logger.Log($"VERBOSE: <color=blue>{msg}</color>");
            else
                logger.Log($"VERBOSE: {msg}");
        }

        [Conditional(SIMPLEWEB_LOG_ENABLED)]
        public static void Info(string msg, bool showColor = true)
        {
            if (level < Levels.info)
                return;

            if (showColor)
                logger.Log($"INFO: <color=blue>{msg}</color>");
            else
                logger.Log($"INFO: {msg}");
        }

        [Conditional(SIMPLEWEB_LOG_ENABLED), Conditional(DEBUG)]
        public static void Warn(string msg, bool showColor = true)
        {
            if (level < Levels.warn)
                return;

            if (showColor)
                logger.LogWarning($"WARN: <color=orange>{msg}</color>");
            else
                logger.LogWarning($"WARN: {msg}");
        }

        [Conditional(SIMPLEWEB_LOG_ENABLED), Conditional(DEBUG)]
        public static void Error(string msg, bool showColor = true)
        {
            if (level < Levels.error)
                return;

            if (showColor)
                logger.LogError($"ERROR: <color=red>{msg}</color>");
            else
                logger.LogError($"ERROR: {msg}");
        }
    }
}
