using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Runs the EditMode tests asynchronously through TestRunnerApi and logs one "[Tests] ..." summary line.
    /// Use this from MCP eval instead of the MCP run_tests command, which blocks the editor's main thread in this project:
    ///   PassportFrenzy.EditorTools.EditModeTestRun.Run();   then read the console / EditModeTestRun.LastSummary.
    /// </summary>
    public static class EditModeTestRun
    {
        public static string LastSummary { get; private set; } = "(not run)";
        public static bool Running { get; private set; }

        [MenuItem("Passport Frenzy/Run EditMode Tests (async)", priority = 40)]
        public static string Run()
        {
            if (Running) return "already running";
            Running = true;
            LastSummary = "(running)";
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "PassportFrenzy.Tests.EditMode" },
            }));
            return "started";
        }

        class Callbacks : ICallbacks
        {
            readonly StringBuilder _failures = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Running = false;
                LastSummary = $"[Tests] passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}" +
                              (_failures.Length > 0 ? "\n" + _failures : "");
                if (result.FailCount > 0) Debug.LogError(LastSummary); else Debug.Log(LastSummary);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed && !result.HasChildren)
                    _failures.AppendLine($"  FAIL {result.FullName}: {result.Message}");
            }
        }
    }
}
