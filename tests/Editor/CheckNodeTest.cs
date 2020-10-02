using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace Mirror.SimpleWeb.Tests
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
            RunNode.Result result = RunNode.Run("HelloWorld.js", false);

            result.AssetTimeout(false);

            result.AssetOutput(
                "Hello World!"
                );
            result.AssetErrors();
        }

        [Test]
        public void ShouldReturnHelloWorld2()
        {
            RunNode.Result result = RunNode.Run("HelloWorld2.js", false);

            result.AssetTimeout(false);
            result.AssetOutput(
                "Hello World!",
                "Hello again World!"
                );
            result.AssetErrors();
        }

        [Test]
        public void ShouldReturnHelloError()
        {
            RunNode.Result result = RunNode.Run("HelloError.js", false);

            result.AssetTimeout(false);
            result.AssetOutput();
            result.AssetErrors(
                "Hello Error!"
                );
        }

        [Test]
        public void ShouldReturnHelloError2()
        {
            RunNode.Result result = RunNode.Run("HelloError2.js", false);

            result.AssetTimeout(false);
            result.AssetOutput();
            result.AssetErrors(
                "Hello Error!",
                "Hello again Error!"
                );
        }

        [Test]
        public void ShouldFinishBeforeTimeout()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            RunNode.Result result = RunNode.Run("HelloWorld.js", false);

            stopwatch.Stop();
            double seconds = stopwatch.Elapsed.TotalSeconds;
            // hello script is fast and should finish faster than 1 second
            Assert.That(seconds, Is.LessThan(2.0));
        }

        [Test]
        public void ShouldStopAfterTimeout()
        {
            RunNode.Result result = RunNode.Run("Timeout.js", false);

            result.AssetTimeout(true);

            result.AssetTimeout(true);
            result.AssetOutput(
                "Should be running"
                );
            result.AssetErrors();
        }
    }
}
