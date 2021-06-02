using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace JamesFrowen.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class BufferPoolTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(-10)]
        public void ErrorWhenSmallestBelow1(int smallest)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                BufferPool bufferPool = new BufferPool(5, smallest, 16384);
            });

            Assert.That(exception.Message, Is.EqualTo("Smallest must be atleast 1"));
        }

        [Test]
        [TestCase(10, 1)]
        [TestCase(100, 99)]
        [TestCase(1, -1)]
        [TestCase(1, 0)]
        public void ErrorWhenLargestBelowSmallest(int smallest, int largest)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                BufferPool bufferPool = new BufferPool(5, smallest, largest);
            });

            Assert.That(exception.Message, Is.EqualTo("Largest must be greater than smallest"));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-10)]
        public void ErrorWhenCountIsBelow2(int count)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            {
                BufferPool bufferPool = new BufferPool(count, 2, 16384);
            });

            Assert.That(exception.Message, Is.EqualTo("Count must be atleast 2"));
        }

        static IEnumerable CreatesCorrectCountSource => Enumerable.Range(2, 100);
        [Test]
        [TestCaseSource(nameof(CreatesCorrectCountSource))]
        public void CreatesCorrectCount(int count)
        {
            BufferPool bufferPool = new BufferPool(count, 2, 16384);

            Assert.That(bufferPool.buckets.Length, Is.EqualTo(count));
        }

        [Test]
        [TestCase(2, 2, 100)]
        [TestCase(5, 2, 16384)]
        [TestCase(8, 2, 16384)]
        [TestCase(15, 2, 16384)]
        [TestCase(15, 100, 200)]
        public void SmallestAndLargestGroupsHavecorrectvalues(int count, int smallest, int largest)
        {
            BufferPool bufferPool = new BufferPool(count, smallest, largest);

            BufferBucket smallestGroup = bufferPool.buckets[0];
            BufferBucket largestGroup = bufferPool.buckets[count - 1];
            Assert.That(smallestGroup.arraySize, Is.EqualTo(smallest));
            // largest can be 1 greater because caclatations should round up
            Assert.That(largestGroup.arraySize, Is.EqualTo(largest).Or.EqualTo(largest + 1), "largest should be equal to larget or large + 1");
        }
    }
}
