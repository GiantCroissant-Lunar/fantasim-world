using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;

namespace Fantasim.Architecture.Router.Tests
{
    public class MockBuildEngine : IBuildEngine
    {
        public List<BuildErrorEventArgs> Errors { get; } = new List<BuildErrorEventArgs>();
        public List<BuildWarningEventArgs> Warnings { get; } = new List<BuildWarningEventArgs>();

        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "";

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
        public void LogMessageEvent(BuildMessageEventArgs e) {}
        public void LogCustomEvent(CustomBuildEventArgs e) {}

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;
        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs, string toolsVersion) => true;
    }

    public class RouterTests
    {
        [Fact]
        public void ConstitutionalDowngrade_FailsBuild()
        {
            var engine = new MockBuildEngine();
            var task = new ArchitectureRouter
            {
                BuildEngine = engine,
                WorldId = "Default",
                Diagnostics = new ITaskItem[]
                {
                    // Simulating an Error from analyzer that Policy says "Allow" (because our mock engine returns Allow by default)
                    new TaskItem("FW1002|Error|Derived|Truth|Symbol")
                }
            };

            // If Policy returns "Allow" (default of empty engine)
            // AND Input is Error
            // AND Id is FW1xxx
            // THEN Router MUST fail.

            bool success = task.Execute();

            Assert.False(success, "Task should fail on constitutional downgrade");
            Assert.Contains(engine.Errors, e => e.Message.Contains("CRITICAL: Attempted to downgrade Constitutional Rule"));
        }
    }
}
