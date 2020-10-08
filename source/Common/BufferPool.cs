using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal interface IBufferOwner
    {
        void Return(ArrayBuffer buffer);
    }

    public struct ArrayBuffer : IDisposable
    {
        public readonly byte[] array;
        private readonly IBufferOwner owner;

        internal ArrayBuffer(IBufferOwner owner, int size)
        {
            this.owner = owner;
            array = new byte[size];
        }

        public void Dispose()
        {
            owner.Return(this);
        }
    }

    internal class BufferBucket : IBufferOwner
    {
        public readonly int arraySize;
        readonly ConcurrentQueue<ArrayBuffer> buffers;

        public BufferBucket(int arraySize)
        {
            this.arraySize = arraySize;
            buffers = new ConcurrentQueue<ArrayBuffer>();
        }

        public ArrayBuffer Take()
        {
            return buffers.TryDequeue(out ArrayBuffer buffer) ? buffer : new ArrayBuffer(this, arraySize);
        }
        public void Return(ArrayBuffer buffer)
        {
            Debug.Assert(buffer.array.Length == arraySize, "Buffer that was returned had an array of the wrong size");
            buffers.Enqueue(buffer);
        }
    }

    /// <summary>
    /// Collection of different sized buffers 
    /// </summary>
    /// <remarks>
    /// Problem:
    /// * need cached byte[] so that new ones arn't created each time.
    /// * arrays sent are multiple different sizes
    /// * some message might be bit so need buffers to cover that size
    /// * most messages will be small compared to max message size
    /// Solution:
    /// * create multiple groups of buffers covering the range of allowed sizes
    /// * split range using math.log so that there are more size groups for small buffers
    /// </remarks>
    public class BufferPool
    {
        internal readonly BufferBucket[] buckets;
        readonly int bucketCount;
        readonly int smallest;
        readonly int largest;

        public BufferPool(int bucketCount, int smallest, int largest)
        {
            if (bucketCount < 2) throw new ArgumentException("Count must be atleast 2");
            if (smallest < 1) throw new ArgumentException("Smallest must be atleast 1");
            if (largest < smallest) throw new ArgumentException("Largest must be greater than smallest");


            this.bucketCount = bucketCount;
            this.smallest = smallest;
            this.largest = largest;


            // split range over log scale (more buckets for smaller sizes)

            double minLog = Math.Log(this.smallest);
            double maxLog = Math.Log(this.largest);

            double range = maxLog - minLog;
            double each = range / (bucketCount - 1);

            double[] sizes = new double[bucketCount];

            for (int i = 0; i < bucketCount; i++)
            {
                sizes[i] = smallest * Math.Pow(Math.E, each * i);
            }

            buckets = new BufferBucket[bucketCount];

            for (int i = 0; i < bucketCount; i++)
            {
                buckets[i] = new BufferBucket((int)Math.Ceiling(sizes[i]));
            }

            // Example
            // 5         count  
            // 20        smallest
            // 16400     largest

            // 3.0       log 20
            // 9.7       log 16400 

            // 6.7       range 9.7 - 3
            // 1.675     each  6.7 / (5-1)

            // 20        e^ (3 + 1.675 * 0)
            // 107       e^ (3 + 1.675 * 1)
            // 572       e^ (3 + 1.675 * 2)
            // 3056      e^ (3 + 1.675 * 3)
            // 16,317    e^ (3 + 1.675 * 4)

            // perceision wont be lose when using doubles
        }

        public ArrayBuffer Take(int size)
        {
            if (size > largest) { throw new ArgumentException($"Size ({size}) is greatest that largest ({largest})"); }

            for (int i = 0; i < bucketCount; i++)
            {
                if (size < buckets[i].arraySize)
                {
                    return buckets[i].Take();
                }
            }

            throw new ArgumentException($"Size ({size}) is greatest that largest ({largest})");
        }
    }
}
