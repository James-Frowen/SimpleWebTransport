#define SIMPLE_WEB_INFO_LOG
using Debug = UnityEngine.Debug;

namespace Mirror.SimpleWeb
{
    public static class Log
    {
        [System.Diagnostics.Conditional("SIMPLE_WEB_INFO_LOG")]
        public static void Info(string msg) => Debug.Log($"<color=blue>{msg}</color>");
    }
}
