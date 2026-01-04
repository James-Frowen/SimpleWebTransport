using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace JamesFrowen.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class SslConfigLoaderTest
    {
        [Test]
        [Ignore("Can't get path to example")]
        public void ExampleIsValid()
        {
            string dir = TransportDir;
            Assert.That(dir, Is.Not.Null.Or.Empty);
            SslConfigLoader.Cert result = SslConfigLoader.LoadCertJson(Path.Combine(dir, ".cert.example.Json"));

            Assert.That(result.path, Is.EqualTo("./certs/MirrorLocal.pfx"));
            Assert.That(result.password, Is.EqualTo(""));
        }

        [Test]
        public void ThrowsIfCantFindJson()
        {
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() =>
            {
                SslConfigLoader.LoadCertJson(Path.Combine(TestDir, "NotARealFile.Json"));
            });

            Assert.That(exception.Message, Does.StartWith("Could not find file "));
        }
        [Test]
        [TestCase(".Bad1.Json")]
        [TestCase(".Bad2.Json")]
        public void ThrowsIfBadJson(string path)
        {
            InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            {
                SslConfigLoader.LoadCertJson(Path.Combine(TestDir, path));
            });

            Assert.That(exception.Message, Does.StartWith("Cert Json didn't not contain \"path\""));
        }

        [Test]
        [TestCase(".Good1.Json", "Some path", "Some password")]
        [TestCase(".Good2.Json", "Some path", "")]
        public void ValidConfig(string jsonPath, string expectedPath, string expectedPassowrd)
        {
            SslConfigLoader.Cert cert = SslConfigLoader.LoadCertJson(Path.Combine(TestDir, jsonPath));
            Assert.That(cert.path, Is.EqualTo(expectedPath));
            Assert.That(cert.password, Is.EqualTo(expectedPassowrd));
        }



        static string _testDir;
        static string TestDir
        {
            get
            {
                findDir(ref _testDir, nameof(SslConfigLoaderTest));
                return _testDir;
            }
        }

        static string _transportDir;
        static string TransportDir
        {
            get
            {
                findDir(ref _transportDir, nameof(SslConfigLoader));
                return _transportDir;
            }
        }

        static void findDir(ref string field, string scriptName)
        {
            if (string.IsNullOrEmpty(field))
            {
                string[] paths = AssetDatabase.FindAssets($"t:Script " + scriptName)
                    .Select(x => AssetDatabase.GUIDToAssetPath(x))
                    .ToArray();
                if (paths.Length == 0)
                {
                    UnityEngine.Debug.LogError("Could not find path of dir");
                    return;
                }

                string match;
                if (paths.Length == 1)
                {
                    match = paths[0];
                }
                else
                {
                    match = paths.FirstOrDefault(x => x.Contains(scriptName + ".cs"));
                    if (match == null)
                        match = paths.First();
                }

                string dir = Path.GetDirectoryName(match);
                field = dir;
            }
        }
    }
}
