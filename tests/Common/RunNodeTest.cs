using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SimpleWebTransport.Tests
{
    public static class RunNode
    {
        public struct Result
        {
            public bool timedOut;
            public string[] output;
            public string[] error;
        }
        public static Result Run(string scriptName, int msTimeout = 5000)
        {
            string fullPath = ResolvePath(scriptName);

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "C:/Program Files/nodejs/node.exe",
                    Arguments = fullPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // needs to be false for redirect
                };

                process.Start();
                StreamReader output = process.StandardOutput;
                StreamReader error = process.StandardError;

                Task waitTask = WaitForEnd(process, msTimeout);
                waitTask.Wait();

                bool timeoutReached = !process.HasExited;
                if (timeoutReached)
                    process.Kill();

                string outputString = output.ReadToEnd();
                string errorString = error.ReadToEnd();

                UnityEngine.Debug.Log($"node outputStream: {outputString}");
                UnityEngine.Debug.Log($"ndoe errorStream: {errorString}");

                return new Result
                {
                    timedOut = timeoutReached,
                    // split and remove empties
                    output = outputString.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                    error = errorString.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                };
            }
        }


        static async Task WaitForEnd(Process process, int msTimeout)
        {
            bool cancel = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(msTimeout).ConfigureAwait(false);

                cancel = true;
            });

            while (!process.HasExited)
            {
                if (cancel)
                {
                    UnityEngine.Debug.Log($"<color=red>NodeRun Timeout reached</color>");

                    return;
                }

                // ConfigureAwait so it runs on side thread
                await Task.Delay(25).ConfigureAwait(false);
            }
        }

        internal static string ResolvePath(string path)
        {
            string full = Path.Combine(NodeDir, path);
            if (full.StartsWith(Application.dataPath))
            {
                full = "Assets" + full.Substring(Application.dataPath.Length);
            }
            return full;
        }
        static string _nodeDir;
        static string NodeDir
        {
            get
            {
                if (string.IsNullOrEmpty(_nodeDir))
                {
                    string[] guidsFound = AssetDatabase.FindAssets($"t:Script " + nameof(RunNode));
                    if (guidsFound.Length == 1 && !string.IsNullOrEmpty(guidsFound[0]))
                    {
                        // tests/common/RunNode.cs
                        string script = AssetDatabase.GUIDToAssetPath(guidsFound[0]);
                        // tests/common/
                        string dir = Path.GetDirectoryName(script);
                        // tests/node~/
                        _nodeDir = Path.Combine(dir, "../", "node~/");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Could not find path of RunNode");
                    }
                }
                return _nodeDir;
            }
        }
    }
}
