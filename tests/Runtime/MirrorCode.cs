// use transport class just for tests, easier than re-writing all tests to work without it
// source: https://github.com/vis2k/Mirror/tree/v30.5.1
/*
MIT License

Copyright (c) 2015, Unity Technologies
Copyright (c) 2019, vis2k, Paul and Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Mirror
{
    public static class Channels
    {
        public static int DefaultReliable = 0;
    }
    /// <summary>
    /// Abstract transport layer component
    /// </summary>
    /// <remarks>
    /// <h2>
    ///   Transport Rules
    /// </h2>
    /// <list type="bullet">
    ///   <listheader><description>
    ///     All transports should follow these rules so that they work correctly with mirror
    ///   </description></listheader>
    ///   <item><description>
    ///     When Monobehaviour is disabled the Transport should not invoke callbacks
    ///   </description></item>
    ///   <item><description>
    ///     Callbacks should be invoked on main thread. It is best to do this from LateUpdate
    ///   </description></item>
    ///   <item><description>
    ///     Callbacks can be invoked after <see cref="ServerStop"/> or <see cref="ClientDisconnect"/> as been called
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="ServerStop"/> or <see cref="ClientDisconnect"/> can be called by mirror multiple times
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="Available"/> should check the platform and 32 vs 64 bit if the transport only works on some of them
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="GetMaxPacketSize"/> should return size even if transport is not running
    ///   </description></item>
    ///   <item><description>
    ///     Default channel should be reliable <see cref="Channels.DefaultReliable"/>
    ///   </description></item>
    /// </list>
    /// </remarks>
    public abstract class Transport : MonoBehaviour
    {
        /// <summary>
        /// The current transport used by Mirror.
        /// </summary>
        public static Transport activeTransport;

        /// <summary>
        /// Is this transport available in the current platform?
        /// <para>Some transports might only be available in mobile</para>
        /// <para>Many will not work in webgl</para>
        /// <para>Example usage: return Application.platform == RuntimePlatform.WebGLPlayer</para>
        /// </summary>
        /// <returns>True if this transport works in the current platform</returns>
        public abstract bool Available();

        #region Client
        /// <summary>
        /// Notify subscribers when when this client establish a successful connection to the server
        /// <para>callback()</para>
        /// </summary>
        public Action OnClientConnected = () => Debug.LogWarning("OnClientConnected called with no handler");

        /// <summary>
        /// Notify subscribers when this client receive data from the server
        /// <para>callback(ArraySegment&lt;byte&gt; data, int channel)</para>
        /// </summary>
        public Action<ArraySegment<byte>, int> OnClientDataReceived = (data, channel) => Debug.LogWarning("OnClientDataReceived called with no handler");

        /// <summary>
        /// Notify subscribers when this client encounters an error communicating with the server
        /// <para>callback(Exception e)</para>
        /// </summary>
        public Action<Exception> OnClientError = (error) => Debug.LogWarning("OnClientError called with no handler");

        /// <summary>
        /// Notify subscribers when this client disconnects from the server
        /// <para>callback()</para>
        /// </summary>
        public Action OnClientDisconnected = () => Debug.LogWarning("OnClientDisconnected called with no handler");

        /// <summary>
        /// Determines if we are currently connected to the server
        /// </summary>
        /// <returns>True if a connection has been established to the server</returns>
        public abstract bool ClientConnected();

        /// <summary>
        /// Establish a connection to a server
        /// </summary>
        /// <param name="address">The IP address or FQDN of the server we are trying to connect to</param>
        public abstract void ClientConnect(string address);

        /// <summary>
        /// Establish a connection to a server
        /// </summary>
        /// <param name="uri">The address of the server we are trying to connect to</param>
        public virtual void ClientConnect(Uri uri)
        {
            // By default, to keep backwards compatibility, just connect to the host
            // in the uri
            ClientConnect(uri.Host);
        }

        /// <summary>
        /// Send data to the server
        /// </summary>
        /// <param name="channelId">The channel to use.  0 is the default channel,
        /// but some transports might want to provide unreliable, encrypted, compressed, or any other feature
        /// as new channels</param>
        /// <param name="segment">The data to send to the server. Will be recycled after returning, so either use it directly or copy it internally. This allows for allocation-free sends!</param>
        public abstract void ClientSend(int channelId, ArraySegment<byte> segment);

        /// <summary>
        /// Disconnect this client from the server
        /// </summary>
        public abstract void ClientDisconnect();

        #endregion

        #region Server


        /// <summary>
        /// Retrieves the address of this server.
        /// Useful for network discovery
        /// </summary>
        /// <returns>the url at which this server can be reached</returns>
        public abstract Uri ServerUri();

        /// <summary>
        /// Notify subscribers when a client connects to this server
        /// <para>callback(int connId)</para>
        /// </summary>
        public Action<int> OnServerConnected = (connId) => Debug.LogWarning("OnServerConnected called with no handler");

        /// <summary>
        /// Notify subscribers when this server receives data from the client
        /// <para>callback(int connId, ArraySegment&lt;byte&gt; data, int channel)</para>
        /// </summary>
        public Action<int, ArraySegment<byte>, int> OnServerDataReceived = (connId, data, channel) => Debug.LogWarning("OnServerDataReceived called with no handler");

        /// <summary>
        /// Notify subscribers when this server has some problem communicating with the client
        /// <para>callback(int connId, Exception e)</para>
        /// </summary>
        public Action<int, Exception> OnServerError = (connId, error) => Debug.LogWarning("OnServerError called with no handler");

        /// <summary>
        /// Notify subscribers when a client disconnects from this server
        /// <para>callback(int connId)</para>
        /// </summary>
        public Action<int> OnServerDisconnected = (connId) => Debug.LogWarning("OnServerDisconnected called with no handler");

        /// <summary>
        /// Determines if the server is up and running
        /// </summary>
        /// <returns>true if the transport is ready for connections from clients</returns>
        public abstract bool ServerActive();

        /// <summary>
        /// Start listening for clients
        /// </summary>
        public abstract void ServerStart();

        /// <summary>
        /// Send data to a client.
        /// </summary>
        /// <param name="connectionId">The client connection id to send the data to</param>
        /// <param name="channelId">The channel to be used.  Transports can use channels to implement
        /// other features such as unreliable, encryption, compression, etc...</param>
        /// <param name="data"></param>
        public abstract void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment);

        /// <summary>
        /// Disconnect a client from this server.  Useful to kick people out.
        /// </summary>
        /// <param name="connectionId">the id of the client to disconnect</param>
        /// <returns>true if the client was kicked</returns>
        public abstract bool ServerDisconnect(int connectionId);

        /// <summary>
        /// Get the client address
        /// </summary>
        /// <param name="connectionId">id of the client</param>
        /// <returns>address of the client</returns>
        public abstract string ServerGetClientAddress(int connectionId);

        /// <summary>
        /// Stop listening for clients and disconnect all existing clients
        /// </summary>
        public abstract void ServerStop();

        #endregion

        /// <summary>
        /// The maximum packet size for a given channel.  Unreliable transports
        /// usually can only deliver small packets. Reliable fragmented channels
        /// can usually deliver large ones.
        ///
        /// GetMaxPacketSize needs to return a value at all times. Even if the
        /// Transport isn't running, or isn't Available(). This is because
        /// Fallback and Multiplex transports need to find the smallest possible
        /// packet size at runtime.
        /// </summary>
        /// <param name="channelId">channel id</param>
        /// <returns>the size in bytes that can be sent via the provided channel</returns>
        public abstract int GetMaxPacketSize(int channelId = 0);

        /// <summary>
        /// The maximum batch(!) size for a given channel.
        /// Uses GetMaxPacketSize by default.
        /// Some transports like kcp support large max packet sizes which should
        /// not be used for batching all the time because they end up being too
        /// slow (head of line blocking etc.).
        /// </summary>
        /// <param name="channelId">channel id</param>
        /// <returns>the size in bytes that should be batched via the provided channel</returns>
        public virtual int GetMaxBatchSize(int channelId) =>
            GetMaxPacketSize(channelId);

        /// <summary>
        /// Shut down the transport, both as client and server
        /// </summary>
        public abstract void Shutdown();

        // block Update() to force Transports to use LateUpdate to avoid race
        // conditions. messages should be processed after all the game state
        // was processed in Update.
        // -> in other words: use LateUpdate!
        // -> uMMORPG 480 CCU stress test: when bot machine stops, it causes
        //    'Observer not ready for ...' log messages when using Update
        // -> occupying a public Update() function will cause Warnings if a
        //    transport uses Update.
        //
        // IMPORTANT: set script execution order to >1000 to call Transport's
        //            LateUpdate after all others. Fixes race condition where
        //            e.g. in uSurvival Transport would apply Cmds before
        //            ShoulderRotation.LateUpdate, resulting in projectile
        //            spawns at the point before shoulder rotation.
#pragma warning disable UNT0001 // Empty Unity message
        public void Update() { }
#pragma warning restore UNT0001 // Empty Unity message

        /// <summary>
        /// called when quitting the application by closing the window / pressing stop in the editor
        /// <para>virtual so that inheriting classes' OnApplicationQuit() can call base.OnApplicationQuit() too</para>
        /// </summary>
        public virtual void OnApplicationQuit()
        {
            // stop transport (e.g. to shut down threads)
            // (when pressing Stop in the Editor, Unity keeps threads alive
            //  until we press Start again. so if Transports use threads, we
            //  really want them to end now and not after next start)
            Shutdown();
        }
    }
}


namespace Mirror
{
    /// <summary>
    /// A class that holds writers for the different types
    /// <para>Note that c# creates a different static variable for each type</para>
    /// <para>This will be populated by the weaver</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Writer<T>
    {
        public static Action<NetworkWriter, T> write;
    }

    /// <summary>
    /// Binary stream Writer. Supports simple types, buffers, arrays, structs, and nested types
    /// <para>Use <see cref="NetworkWriterPool.GetWriter">NetworkWriter.GetWriter</see> to reduce memory allocation</para>
    /// </summary>
    public class NetworkWriter
    {
        public const int MaxStringLength = 1024 * 32;

        // create writer immediately with it's own buffer so no one can mess with it and so that we can resize it.
        // note: BinaryWriter allocates too much, so we only use a MemoryStream
        // => 1500 bytes by default because on average, most packets will be <= MTU
        byte[] buffer = new byte[1500];

        // 'int' is the best type for .Position. 'short' is too small if we send >32kb which would result in negative .Position
        // -> converting long to int is fine until 2GB of data (MAX_INT), so we don't have to worry about overflows here
        int position;
        int length;

        /// <summary>
        /// Number of bytes writen to the buffer
        /// </summary>
        public int Length => length;

        /// <summary>
        /// Next position to write to the buffer
        /// </summary>
        public int Position
        {
            get => position;
            set
            {
                position = value;
                EnsureLength(value);
            }
        }

        /// <summary>
        /// Reset both the position and length of the stream
        /// </summary>
        /// <remarks>
        /// Leaves the capacity the same so that we can reuse this writer without extra allocations
        /// </remarks>
        public void Reset()
        {
            position = 0;
            length = 0;
        }

        /// <summary>
        /// Sets length, moves position if it is greater than new length
        /// </summary>
        /// <param name="newLength"></param>
        /// <remarks>
        /// Zeros out any extra length created by setlength
        /// </remarks>
        public void SetLength(int newLength)
        {
            int oldLength = length;

            // ensure length & capacity
            EnsureLength(newLength);

            // zero out new length
            if (oldLength < newLength)
            {
                Array.Clear(buffer, oldLength, newLength - oldLength);
            }

            length = newLength;
            position = Mathf.Min(position, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureLength(int value)
        {
            if (length < value)
            {
                length = value;
                EnsureCapacity(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureCapacity(int value)
        {
            if (buffer.Length < value)
            {
                int capacity = Math.Max(value, buffer.Length * 2);
                Array.Resize(ref buffer, capacity);
            }
        }

        /// <summary>
        /// Copys buffer to new array with the size of <see cref="Length"/>
        /// <para></para>
        /// </summary>
        /// <returns>all the data we have written, regardless of the current position</returns>
        public byte[] ToArray()
        {
            byte[] data = new byte[length];
            Array.ConstrainedCopy(buffer, 0, data, 0, length);
            return data;
        }

        /// <summary>
        /// Create an ArraySegment using the buffer and <see cref="Length"/>
        /// <para>
        ///     Dont modify the NetworkWriter while using the ArraySegment as this can overwrite the bytes
        /// </para>
        /// <para>
        ///     Use ToArraySegment instead of ToArray to avoid allocations
        /// </para>
        /// </summary>
        /// <returns>all the data we have written, regardless of the current position</returns>
        public ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(buffer, 0, length);
        }

        public void WriteByte(byte value)
        {
            EnsureLength(position + 1);
            buffer[position++] = value;
        }

        // for byte arrays with consistent size, where the reader knows how many to read
        // (like a packet opcode that's always the same)
        public void WriteBytes(byte[] buffer, int offset, int count)
        {
            EnsureLength(position + count);
            Array.ConstrainedCopy(buffer, offset, this.buffer, position, count);
            position += count;
        }
    }
    // -- helpers for float conversion without allocations --
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float floatValue;

        [FieldOffset(0)]
        public uint intValue;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDouble
    {
        [FieldOffset(0)]
        public double doubleValue;

        [FieldOffset(0)]
        public ulong longValue;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDecimal
    {
        [FieldOffset(0)]
        public ulong longValue1;

        [FieldOffset(8)]
        public ulong longValue2;

        [FieldOffset(0)]
        public decimal decimalValue;
    }

    // Mirror's Weaver automatically detects all NetworkWriter function types,
    // but they do all need to be extensions.
    public static class NetworkWriterExtensions
    {

        // cache encoding instead of creating it with BinaryWriter each time
        // 1000 readers before:  1MB GC, 30ms
        // 1000 readers after: 0.8MB GC, 18ms
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);
        static readonly byte[] stringBuffer = new byte[NetworkWriter.MaxStringLength];

        public static void WriteByte(this NetworkWriter writer, byte value) => writer.WriteByte(value);

        public static void WriteSByte(this NetworkWriter writer, sbyte value) => writer.WriteByte((byte)value);

        public static void WriteChar(this NetworkWriter writer, char value) => writer.WriteUInt16(value);

        public static void WriteBoolean(this NetworkWriter writer, bool value) => writer.WriteByte((byte)(value ? 1 : 0));

        public static void WriteUInt16(this NetworkWriter writer, ushort value)
        {
            writer.WriteByte((byte)value);
            writer.WriteByte((byte)(value >> 8));
        }

        public static void WriteInt16(this NetworkWriter writer, short value) => writer.WriteUInt16((ushort)value);

        public static void WriteUInt32(this NetworkWriter writer, uint value)
        {
            writer.WriteByte((byte)value);
            writer.WriteByte((byte)(value >> 8));
            writer.WriteByte((byte)(value >> 16));
            writer.WriteByte((byte)(value >> 24));
        }

        public static void WriteInt32(this NetworkWriter writer, int value) => writer.WriteUInt32((uint)value);

        public static void WriteUInt64(this NetworkWriter writer, ulong value)
        {
            writer.WriteByte((byte)value);
            writer.WriteByte((byte)(value >> 8));
            writer.WriteByte((byte)(value >> 16));
            writer.WriteByte((byte)(value >> 24));
            writer.WriteByte((byte)(value >> 32));
            writer.WriteByte((byte)(value >> 40));
            writer.WriteByte((byte)(value >> 48));
            writer.WriteByte((byte)(value >> 56));
        }

        public static void WriteInt64(this NetworkWriter writer, long value) => writer.WriteUInt64((ulong)value);

        public static void WriteSingle(this NetworkWriter writer, float value)
        {
            var converter = new UIntFloat
            {
                floatValue = value
            };
            writer.WriteUInt32(converter.intValue);
        }

        public static void WriteDouble(this NetworkWriter writer, double value)
        {
            var converter = new UIntDouble
            {
                doubleValue = value
            };
            writer.WriteUInt64(converter.longValue);
        }

        public static void WriteDecimal(this NetworkWriter writer, decimal value)
        {
            // the only way to read it without allocations is to both read and
            // write it with the FloatConverter (which is not binary compatible
            // to writer.Write(decimal), hence why we use it here too)
            var converter = new UIntDecimal
            {
                decimalValue = value
            };
            writer.WriteUInt64(converter.longValue1);
            writer.WriteUInt64(converter.longValue2);
        }

        public static void WriteString(this NetworkWriter writer, string value)
        {
            // write 0 for null support, increment real size by 1
            // (note: original HLAPI would write "" for null strings, but if a
            //        string is null on the server then it should also be null
            //        on the client)
            if (value == null)
            {
                writer.WriteUInt16(0);
                return;
            }

            // write string with same method as NetworkReader
            // convert to byte[]
            int size = encoding.GetBytes(value, 0, value.Length, stringBuffer, 0);

            // check if within max size
            if (size >= NetworkWriter.MaxStringLength)
            {
                throw new IndexOutOfRangeException("NetworkWriter.Write(string) too long: " + size + ". Limit: " + NetworkWriter.MaxStringLength);
            }

            // write size and bytes
            writer.WriteUInt16(checked((ushort)(size + 1)));
            writer.WriteBytes(stringBuffer, 0, size);
        }

        // for byte arrays with dynamic size, where the reader doesn't know how many will come
        // (like an inventory with different items etc.)
        public static void WriteBytesAndSize(this NetworkWriter writer, byte[] buffer, int offset, int count)
        {
            // null is supported because [SyncVar]s might be structs with null byte[] arrays
            // write 0 for null array, increment normal size by 1 to save bandwith
            // (using size=-1 for null would limit max size to 32kb instead of 64kb)
            if (buffer == null)
            {
                writer.WriteUInt32(0u);
                return;
            }
            writer.WriteUInt32(checked((uint)count) + 1u);
            writer.WriteBytes(buffer, offset, count);
        }

        // Weaver needs a write function with just one byte[] parameter
        // (we don't name it .Write(byte[]) because it's really a WriteBytesAndSize since we write size / null info too)
        public static void WriteBytesAndSize(this NetworkWriter writer, byte[] buffer)
        {
            // buffer might be null, so we can't use .Length in that case
            writer.WriteBytesAndSize(buffer, 0, buffer != null ? buffer.Length : 0);
        }

        public static void WriteBytesAndSizeSegment(this NetworkWriter writer, ArraySegment<byte> buffer)
        {
            writer.WriteBytesAndSize(buffer.Array, buffer.Offset, buffer.Count);
        }

        public static void WriteVector2(this NetworkWriter writer, Vector2 value)
        {
            writer.WriteSingle(value.x);
            writer.WriteSingle(value.y);
        }

        public static void WriteVector3(this NetworkWriter writer, Vector3 value)
        {
            writer.WriteSingle(value.x);
            writer.WriteSingle(value.y);
            writer.WriteSingle(value.z);
        }

        public static void WriteVector4(this NetworkWriter writer, Vector4 value)
        {
            writer.WriteSingle(value.x);
            writer.WriteSingle(value.y);
            writer.WriteSingle(value.z);
            writer.WriteSingle(value.w);
        }

        public static void WriteVector2Int(this NetworkWriter writer, Vector2Int value)
        {
            writer.WriteInt32(value.x);
            writer.WriteInt32(value.y);
        }

        public static void WriteVector3Int(this NetworkWriter writer, Vector3Int value)
        {
            writer.WriteInt32(value.x);
            writer.WriteInt32(value.y);
            writer.WriteInt32(value.z);
        }

        public static void WriteColor(this NetworkWriter writer, Color value)
        {
            writer.WriteSingle(value.r);
            writer.WriteSingle(value.g);
            writer.WriteSingle(value.b);
            writer.WriteSingle(value.a);
        }

        public static void WriteColor32(this NetworkWriter writer, Color32 value)
        {
            writer.WriteByte(value.r);
            writer.WriteByte(value.g);
            writer.WriteByte(value.b);
            writer.WriteByte(value.a);
        }

        public static void WriteQuaternion(this NetworkWriter writer, Quaternion value)
        {
            writer.WriteSingle(value.x);
            writer.WriteSingle(value.y);
            writer.WriteSingle(value.z);
            writer.WriteSingle(value.w);
        }

        public static void WriteRect(this NetworkWriter writer, Rect value)
        {
            writer.WriteSingle(value.xMin);
            writer.WriteSingle(value.yMin);
            writer.WriteSingle(value.width);
            writer.WriteSingle(value.height);
        }

        public static void WritePlane(this NetworkWriter writer, Plane value)
        {
            writer.WriteVector3(value.normal);
            writer.WriteSingle(value.distance);
        }

        public static void WriteRay(this NetworkWriter writer, Ray value)
        {
            writer.WriteVector3(value.origin);
            writer.WriteVector3(value.direction);
        }

        public static void WriteMatrix4x4(this NetworkWriter writer, Matrix4x4 value)
        {
            writer.WriteSingle(value.m00);
            writer.WriteSingle(value.m01);
            writer.WriteSingle(value.m02);
            writer.WriteSingle(value.m03);
            writer.WriteSingle(value.m10);
            writer.WriteSingle(value.m11);
            writer.WriteSingle(value.m12);
            writer.WriteSingle(value.m13);
            writer.WriteSingle(value.m20);
            writer.WriteSingle(value.m21);
            writer.WriteSingle(value.m22);
            writer.WriteSingle(value.m23);
            writer.WriteSingle(value.m30);
            writer.WriteSingle(value.m31);
            writer.WriteSingle(value.m32);
            writer.WriteSingle(value.m33);
        }

        public static void WriteGuid(this NetworkWriter writer, Guid value)
        {
            byte[] data = value.ToByteArray();
            writer.WriteBytes(data, 0, data.Length);
        }

        public static void WriteUri(this NetworkWriter writer, Uri uri)
        {
            writer.WriteString(uri.ToString());
        }
    }
}

namespace Mirror
{
    public static class StringHash
    {
        // string.GetHashCode is not guaranteed to be the same on all machines, but
        // we need one that is the same on all machines. simple and stupid:
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
namespace Mirror
{
    public interface NetworkMessage { }
    // message packing all in one place, instead of constructing headers in all
    // kinds of different places
    //
    //   MsgType     (1-n bytes)
    //   Content     (ContentSize bytes)
    //
    // -> we use varint for headers because most messages will result in 1 byte
    //    type/size headers then instead of always
    //    using 2 bytes for shorts.
    // -> this reduces bandwidth by 10% if average message size is 20 bytes
    //    (probably even shorter)
    public static class MessagePacker
    {
        /// <summary>
        /// this is the minimum size of a message that mirror will accept
        /// </summary>
        internal const int HeaderSize = sizeof(ushort);

        public static int GetId<T>() where T : struct, NetworkMessage
        {
            // paul: 16 bits is enough to avoid collisions
            //  - keeps the message size small because it gets varinted
            //  - in case of collisions,  Mirror will display an error
            return typeof(T).FullName.GetStableHashCode() & 0xFFFF;
        }

        // unpack message after receiving
        // -> pass NetworkReader so it's less strange if we create it in here
        //    and pass it upwards.
        // -> NetworkReader will point at content afterwards!
        public static bool Unpack(NetworkReader messageReader, out int msgType)
        {
            // read message type (varint)
            try
            {
                msgType = messageReader.ReadUInt16();
                return true;
            }
            catch (System.IO.EndOfStreamException)
            {
                msgType = 0;
                return false;
            }
        }
    }
}

namespace Mirror
{
    /// <summary>
    /// a class that holds readers for the different types
    /// Note that c# creates a different static variable for each
    /// type
    /// This will be populated by the weaver
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Reader<T>
    {
        public static Func<NetworkReader, T> read;
    }

    /// <summary>
    /// Binary stream Reader. Supports simple types, buffers, arrays, structs, and nested types
    /// <para>Use <see cref="NetworkReaderPool.GetReader">NetworkReaderPool.GetReader</see> to reduce memory allocation</para>
    /// <para>
    /// Note: This class is intended to be extremely pedantic,
    /// and throw exceptions whenever stuff is going slightly wrong.
    /// The exceptions will be handled in NetworkServer/NetworkClient.
    /// </para>
    /// </summary>
    public class NetworkReader
    {
        // Custom NetworkReader that doesn't use C#'s built in MemoryStream in order to
        // avoid allocations.
        //
        // Benchmark: 100kb byte[] passed to NetworkReader constructor 1000x
        //   before with MemoryStream
        //     0.8% CPU time, 250KB memory, 3.82ms
        //   now:
        //     0.0% CPU time,  32KB memory, 0.02ms

        // internal buffer
        // byte[] pointer would work, but we use ArraySegment to also support
        // the ArraySegment constructor
        internal ArraySegment<byte> buffer;

        // 'int' is the best type for .Position. 'short' is too small if we send >32kb which would result in negative .Position
        // -> converting long to int is fine until 2GB of data (MAX_INT), so we don't have to worry about overflows here
        /// <summary>
        /// Next position to read from the buffer
        /// </summary>
        public int Position;

        /// <summary>
        /// Total number of bytes to read from buffer
        /// </summary>
        public int Length => buffer.Count;

        public NetworkReader(byte[] bytes)
        {
            buffer = new ArraySegment<byte>(bytes);
        }

        public NetworkReader(ArraySegment<byte> segment)
        {
            buffer = segment;
        }

        public byte ReadByte()
        {
            if (Position + 1 > buffer.Count)
            {
                throw new EndOfStreamException("ReadByte out of range:" + ToString());
            }
            return buffer.Array[buffer.Offset + Position++];
        }

        /// <summary>
        /// read bytes into <paramref name="bytes"/>
        /// </summary>
        /// <returns><paramref name="bytes"/></returns>
        public byte[] ReadBytes(byte[] bytes, int count)
        {
            // check if passed byte array is big enough
            if (count > bytes.Length)
            {
                throw new EndOfStreamException("ReadBytes can't read " + count + " + bytes because the passed byte[] only has length " + bytes.Length);
            }

            ArraySegment<byte> data = ReadBytesSegment(count);
            Array.Copy(data.Array, data.Offset, bytes, 0, count);
            return bytes;
        }

        /// <summary>
        /// Create Segment from current position
        /// <para>
        ///     Useful to parse payloads etc. without allocating
        /// </para>
        /// </summary>
        public ArraySegment<byte> ReadBytesSegment(int count)
        {
            // check if within buffer limits
            if (Position + count > buffer.Count)
            {
                throw new EndOfStreamException("ReadBytesSegment can't read " + count + " bytes because it would read past the end of the stream. " + ToString());
            }

            // return the segment
            var result = new ArraySegment<byte>(buffer.Array, buffer.Offset + Position, count);
            Position += count;
            return result;
        }

        /// <returns>Information about reader: pos, len, buffer contents</returns>
        public override string ToString()
        {
            return $"NetworkReader pos={Position} len={Length} buffer={BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count)}";
        }
    }


    // Mirror's Weaver automatically detects all NetworkReader function types,
    // but they do all need to be extensions.
    public static class NetworkReaderExtensions
    {
        // cache encoding instead of creating it each time
        // 1000 readers before:  1MB GC, 30ms
        // 1000 readers after: 0.8MB GC, 18ms
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

        public static byte ReadByte(this NetworkReader reader) => reader.ReadByte();
        public static sbyte ReadSByte(this NetworkReader reader) => (sbyte)reader.ReadByte();
        public static char ReadChar(this NetworkReader reader) => (char)reader.ReadUInt16();
        public static bool ReadBoolean(this NetworkReader reader) => reader.ReadByte() != 0;
        public static short ReadInt16(this NetworkReader reader) => (short)reader.ReadUInt16();
        public static ushort ReadUInt16(this NetworkReader reader)
        {
            ushort value = 0;
            value |= reader.ReadByte();
            value |= (ushort)(reader.ReadByte() << 8);
            return value;
        }
        public static int ReadInt32(this NetworkReader reader) => (int)reader.ReadUInt32();
        public static uint ReadUInt32(this NetworkReader reader)
        {
            uint value = 0;
            value |= reader.ReadByte();
            value |= (uint)(reader.ReadByte() << 8);
            value |= (uint)(reader.ReadByte() << 16);
            value |= (uint)(reader.ReadByte() << 24);
            return value;
        }
        public static long ReadInt64(this NetworkReader reader) => (long)reader.ReadUInt64();
        public static ulong ReadUInt64(this NetworkReader reader)
        {
            ulong value = 0;
            value |= reader.ReadByte();
            value |= ((ulong)reader.ReadByte()) << 8;
            value |= ((ulong)reader.ReadByte()) << 16;
            value |= ((ulong)reader.ReadByte()) << 24;
            value |= ((ulong)reader.ReadByte()) << 32;
            value |= ((ulong)reader.ReadByte()) << 40;
            value |= ((ulong)reader.ReadByte()) << 48;
            value |= ((ulong)reader.ReadByte()) << 56;
            return value;
        }
        public static float ReadSingle(this NetworkReader reader)
        {
            var converter = new UIntFloat();
            converter.intValue = reader.ReadUInt32();
            return converter.floatValue;
        }
        public static double ReadDouble(this NetworkReader reader)
        {
            var converter = new UIntDouble();
            converter.longValue = reader.ReadUInt64();
            return converter.doubleValue;
        }
        public static decimal ReadDecimal(this NetworkReader reader)
        {
            var converter = new UIntDecimal();
            converter.longValue1 = reader.ReadUInt64();
            converter.longValue2 = reader.ReadUInt64();
            return converter.decimalValue;
        }

        /// <exception cref="T:System.ArgumentException">if an invalid utf8 string is sent</exception>
        public static string ReadString(this NetworkReader reader)
        {
            // read number of bytes
            ushort size = reader.ReadUInt16();

            // null support, see NetworkWriter
            if (size == 0)
                return null;

            int realSize = size - 1;

            // make sure it's within limits to avoid allocation attacks etc.
            if (realSize >= NetworkWriter.MaxStringLength)
            {
                throw new EndOfStreamException("ReadString too long: " + realSize + ". Limit is: " + NetworkWriter.MaxStringLength);
            }

            ArraySegment<byte> data = reader.ReadBytesSegment(realSize);

            // convert directly from buffer to string via encoding
            return encoding.GetString(data.Array, data.Offset, data.Count);
        }

        /// <exception cref="T:OverflowException">if count is invalid</exception>
        public static byte[] ReadBytesAndSize(this NetworkReader reader)
        {
            // count = 0 means the array was null
            // otherwise count -1 is the length of the array
            uint count = reader.ReadUInt32();
            // Use checked() to force it to throw OverflowException if data is invalid
            return count == 0 ? null : reader.ReadBytes(checked((int)(count - 1u)));
        }

        /// <exception cref="T:OverflowException">if count is invalid</exception>
        public static ArraySegment<byte> ReadBytesAndSizeSegment(this NetworkReader reader)
        {
            // count = 0 means the array was null
            // otherwise count - 1 is the length of the array
            uint count = reader.ReadUInt32();
            // Use checked() to force it to throw OverflowException if data is invalid
            return count == 0 ? default : reader.ReadBytesSegment(checked((int)(count - 1u)));
        }

        public static Vector2 ReadVector2(this NetworkReader reader) => new Vector2(reader.ReadSingle(), reader.ReadSingle());
        public static Vector3 ReadVector3(this NetworkReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Vector4 ReadVector4(this NetworkReader reader) => new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Vector2Int ReadVector2Int(this NetworkReader reader) => new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        public static Vector3Int ReadVector3Int(this NetworkReader reader) => new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        public static Color ReadColor(this NetworkReader reader) => new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Color32 ReadColor32(this NetworkReader reader) => new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        public static Quaternion ReadQuaternion(this NetworkReader reader) => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Rect ReadRect(this NetworkReader reader) => new Rect(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static Plane ReadPlane(this NetworkReader reader) => new Plane(reader.ReadVector3(), reader.ReadSingle());
        public static Ray ReadRay(this NetworkReader reader) => new Ray(reader.ReadVector3(), reader.ReadVector3());

        public static Matrix4x4 ReadMatrix4x4(this NetworkReader reader)
        {
            return new Matrix4x4
            {
                m00 = reader.ReadSingle(),
                m01 = reader.ReadSingle(),
                m02 = reader.ReadSingle(),
                m03 = reader.ReadSingle(),
                m10 = reader.ReadSingle(),
                m11 = reader.ReadSingle(),
                m12 = reader.ReadSingle(),
                m13 = reader.ReadSingle(),
                m20 = reader.ReadSingle(),
                m21 = reader.ReadSingle(),
                m22 = reader.ReadSingle(),
                m23 = reader.ReadSingle(),
                m30 = reader.ReadSingle(),
                m31 = reader.ReadSingle(),
                m32 = reader.ReadSingle(),
                m33 = reader.ReadSingle()
            };
        }

        public static byte[] ReadBytes(this NetworkReader reader, int count)
        {
            byte[] bytes = new byte[count];
            reader.ReadBytes(bytes, count);
            return bytes;
        }

        public static Guid ReadGuid(this NetworkReader reader) => new Guid(reader.ReadBytes(16));

        public static Uri ReadUri(this NetworkReader reader)
        {
            return new Uri(reader.ReadString());
        }
    }
}

namespace Mirror
{
    public struct RpcMessage : NetworkMessage
    {
        public uint netId;
        public int componentIndex;
        public int functionHash;
        // the parameters for the Cmd function
        // -> ArraySegment to avoid unnecessary allocations
        public ArraySegment<byte> payload;

        public void Write(NetworkWriter writer)
        {
            writer.WriteUInt32(netId);
            writer.WriteByte((byte)componentIndex);
            writer.WriteInt32(functionHash);
            writer.WriteBytesAndSizeSegment(payload);
        }
    }
}
