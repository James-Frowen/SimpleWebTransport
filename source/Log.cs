#define SIMPLE_WEB_INFO_LOG
using Debug = UnityEngine.Debug;

namespace Mirror.SimpleWeb
{
    public static class Log
    {
        [System.Diagnostics.Conditional("SIMPLE_WEB_INFO_LOG")]
        public static void Info(string msg) => Debug.Log($"INFO: <color=blue>{msg}</color>");

        [System.Diagnostics.Conditional("SIMPLE_WEB_INFO_LOG")]
        public static void Info(string msg, bool showColor)
        {
            if (showColor)
                Info(msg);
            else
                Debug.Log($"INFO: {msg}");
        }
    }
}
