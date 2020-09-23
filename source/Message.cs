#define SIMPLE_WEB_INFO_LOG
using System;

namespace Mirror.SimpleWeb
{
    public struct Message
    {
        public int connId;
        public EventType type;
        public ArraySegment<byte> data;
        public Exception exception;
    }
}
