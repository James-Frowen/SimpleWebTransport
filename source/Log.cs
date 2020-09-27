using Debug = UnityEngine.Debug;

namespace Mirror.SimpleWeb
{
    public static class Log
    {
        public static bool enabled = false;

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Info(string msg)
        {
            if (!enabled)
                return;

            Debug.Log($"INFO: <color=blue>{msg}</color>");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Info(string msg, bool showColor)
        {
            if (!enabled)
                return;

            if (showColor)
                Info(msg);
            else
                Debug.Log($"INFO: {msg}");
        }
    }
}
