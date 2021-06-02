using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace JamesFrowen.SimpleWeb.Benchmark
{
    public class CopyBufferBenchmark : MonoBehaviour
    {
        private void Start()
        {
            //int[] sizes = new int[] { 10, 100, 800, 2000, 16000, ushort.MaxValue };
            int[] smallSizes = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, ushort.MaxValue };
            int minIterations = 10000;
            testOne(nameof(copyMethod), copyMethod, smallSizes, minIterations);
            testOne(nameof(forLoop), forLoop, smallSizes, minIterations);
            testOne(nameof(ArrayCopy), ArrayCopy, smallSizes, minIterations);
            testOne(nameof(BufferCopy), BufferCopy, smallSizes, minIterations);

            Application.Quit();
        }

        void testOne(string name, Action<byte[], byte[], int> action, int[] sizes, int minIterations)
        {
            int max = sizes.Max();
            int itterSize = minIterations * max;
            foreach (int size in sizes)
            {
                byte[] src = new byte[size];
                byte[] dst = new byte[size];

                int itt = itterSize / size;
                int warmup = itt / 10;

                for (int i = 0; i < warmup; i++)
                {
                    action.Invoke(src, dst, size);
                }
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < itt; i++)
                {
                    action.Invoke(src, dst, size);
                }
                sw.Stop();
                Console.WriteLine($"{name,-20}{size,-20}{sw.ElapsedMilliseconds}");
            }
        }

        void copyMethod(byte[] src, byte[] dst, int length)
        {
            forLoop(src, dst, length);
        }
        void forLoop(byte[] src, byte[] dst, int length)
        {
            for (int i = 0; i < length; i++)
            {
                dst[i] = src[i];
            }
        }
        void ArrayCopy(byte[] src, byte[] dst, int length)
        {
            Array.Copy(src, 0, dst, 0, length);
        }
        void BufferCopy(byte[] src, byte[] dst, int length)
        {
            Buffer.BlockCopy(src, 0, dst, 0, length);
        }
    }
}
