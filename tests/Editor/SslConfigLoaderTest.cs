using System.IO;
using NUnit.Framework;
using UnityEditor;

namespace Mirror.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class SslConfigLoaderTest
    {
        [Test]
        public void ExampleIsValid()
        {
            SslConfigLoader.Cert result = SslConfigLoader.LoadCertJson("./Assets/SimpleWebTransport/cert.example.Json");

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

            Assert.That(exception.Message, Does.StartWith("Cert Json didnt not contain \"path\""));
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
                if (string.IsNullOrEmpty(_testDir))
                {
                    string[] guidsFound = AssetDatabase.FindAssets($"t:Script " + nameof(SslConfigLoaderTest));
                    if (guidsFound.Length == 1 && !string.IsNullOrEmpty(guidsFound[0]))
                    {
                        string script = AssetDatabase.GUIDToAssetPath(guidsFound[0]);
                        string dir = Path.GetDirectoryName(script);
                        _testDir = dir;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Could not find path of TestDir");
                    }
                }
                return _testDir;
            }
        }
    }
}
