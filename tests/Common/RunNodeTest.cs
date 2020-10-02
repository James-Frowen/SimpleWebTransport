using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Mirror.SimpleWeb.Tests
{
    public static class RunNode
    {
        static Thread mainThread = Thread.CurrentThread;

        public struct Result
        {
            public bool timedOut;
            public string[] output;
            public string[] error;

            public void AssetOutput(params string[] messages)
            {
                AssertLogs(nameof(output), output, messages);
            }

            public void AssetErrors(params string[] messages)
            {
                AssertLogs(nameof(error), error, messages);
            }

            static void AssertLogs(string label, string[] logs, string[] expected)
            {
                int length = expected.Length;
                Assert.That(logs, Has.Length.EqualTo(length), $"{label} should have {length} logs. Logs: \n{WriteLogs(logs)}");

                for (int i = 0; i < length; i++)
                {
                    Assert.That(logs[i].ToLower(), Is.EqualTo(expected[i].ToLower()));
                }
            }

            static string WriteLogs(string[] lines)
            {
                IEnumerable<int> range = Enumerable.Range(0, lines.Length);
                return string.Join("", Enumerable.Zip(range, lines, (i, line) => $"{i}: {line}\n"));
            }

            public void AssetTimeout(bool expected)
            {
                Assert.That(timedOut, Is.EqualTo(expected),
                    expected
                    ? "nodejs should have timed out"
                    : "nodejs should close before timeout"
                    );
            }

            public void AssetOutputUnordered(string[] messages)
            {
                AssertLogsUnordered(nameof(output), output, messages);
            }

            static void AssertLogsUnordered(string label, string[] logs, string[] expected)
            {
                int length = expected.Length;
                Assert.That(logs, Has.Length.EqualTo(length), $"{label} should have {length} logs. Logs: \n{WriteLogs(logs)}");

                CollectionAssert.AreEquivalent(expected, logs);
            }
        }

        public static Result Run(string scriptName, bool continueOnCapturedContext, int msTimeout = 5000)
        {
            Task<RunNode.Result> task = RunAsync(scriptName, msTimeout, continueOnCapturedContext);
            task.Wait();
            return task.Result;
        }
        public static Task<Result> RunAsync(string scriptName, string arg0, int msTimeout = 5000, bool continueOnCapturedContext = true)
        {
            string[] args = arg0 != null ? new string[] { arg0 } : null;
            return RunAsync(scriptName, msTimeout, continueOnCapturedContext, args);
        }
        public static async Task<Result> RunAsync(string scriptName, int msTimeout = 5000, bool continueOnCapturedContext = true, string[] args = null)
        {
            // run at start so is on main thread
            initNodeDir();
            string fullPath = ResolvePath(scriptName);

            // task.run will run in side thread
            Task<Result> task = Task.Run<Result>(async () =>
            {
                try
                {
                    string argString = args != null ? string.Join(" ", args) : "";

                    using (Process process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = @"C:\Program Files\nodejs\node.exe",
                            Arguments = $"--no-warnings {fullPath} {argString}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false, // needs to be false for redirect,
                        };

                        process.Start();

                        (bool timeoutReached, string outputString, string errorString) = await WaitAndRead(process, msTimeout).ConfigureAwait(continueOnCapturedContext);



                        UnityEngine.Debug.Log($"node outputStream: {outputString}");
                        UnityEngine.Debug.Log($"node errorStream: {errorString}");

                        return new Result
                        {
                            timedOut = timeoutReached,
                            // split and remove empties
                            output = outputString.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                            error = errorString.Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray(),
                        };
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    return default;
                }
            });

            Result result = await task.ConfigureAwait(continueOnCapturedContext);
            return result;
        }

        static async Task<(bool timeouted, string outputString, string errorString)> WaitAndRead(Process process, int msTimeout)
        {
            bool cancel = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(msTimeout).ConfigureAwait(false);

                cancel = true;
            });


            Task<(string outputString, string errorString)> readAsyncTask = Task.Run(() => ReadyAllFrom(process));


            bool timeoutReached = false;
            while (!process.HasExited)
            {
                if (cancel)
                {
                    UnityEngine.Debug.Log($"<color=red>NodeRun Timeout reached</color>");

                    timeoutReached = !process.HasExited;
                    if (timeoutReached)
                        process.Kill();
                }

                // ConfigureAwait so it runs on side thread
                await Task.Delay(25).ConfigureAwait(false);
            }

            (string outputString, string errorString) = await readAsyncTask;

            return (timeoutReached, outputString, errorString);
        }

        private static (string outputString, string errorString) ReadyAllFrom(Process process)
        {
            StreamReader output = process.StandardOutput;
            StreamReader error = process.StandardError;
            string outputString = "";
            string errorString = "";

            while (!process.HasExited)
            {
                outputString += output.ReadToEnd();
                errorString += error.ReadToEnd();
            }

            // read 1 last time to make sure all is read
            outputString += output.ReadToEnd();
            errorString += error.ReadToEnd();

            return (outputString, errorString);
        }

        public static string ResolvePath(string path)
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
                initNodeDir();
                return _nodeDir;
            }
        }
        private static void initNodeDir()
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
        }
    }
}
