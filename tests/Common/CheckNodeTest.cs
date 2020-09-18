using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace SimpleWebTransport.Tests
{
    [Category("SimpleWebTransport")]
    public class CheckNodeTest
    {
        /// <summary>
        /// This test will fail if SimpleWebTransport isnt in root of project
        /// </summary>
        [Test]
        public void FindFullPath()
        {
            string actual = RunNode.ResolvePath("HelloWorld.js");
            string expected = "./Assets/SimpleWebTransport/tests/node~/HelloWorld.js";

            Assert.That(Path.GetFullPath(actual), Is.EqualTo(Path.GetFullPath(expected)));
        }

        [Test]
        public void ShouldReturnHelloWorld()
        {
            RunNode.Result result = RunNode.Run("HelloWorld.js");

            Assert.That(result.timedOut, Is.False);

            Assert.That(result.output, Has.Length.EqualTo(1));
            Assert.That(result.output[0], Is.EqualTo("Hello World!"));

            Assert.That(result.error, Has.Length.EqualTo(0));
        }

        [Test]
        public void ShouldFinishBeforeTimeout()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            RunNode.Result result = RunNode.Run("HelloWorld.js");

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            // hello script is fast and should finish faster than 1 second
            Assert.That(seconds, Is.LessThan(2.0));
        }

        [Test]
        public void ShouldStopAfterTimeout()
        {
            RunNode.Result result = RunNode.Run("Timeout.js");

            Assert.That(result.timedOut, Is.True);

            Assert.That(result.output, Has.Length.EqualTo(1));
            Assert.That(result.output[0], Is.EqualTo("Should be running"));

            Assert.That(result.error, Has.Length.EqualTo(0));
        }
    }
}
