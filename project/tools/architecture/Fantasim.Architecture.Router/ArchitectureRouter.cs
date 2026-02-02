using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;
using Fantasim.Architecture.Policy;

namespace Fantasim.Architecture.Router
{
    public class ArchitectureRouter : MSBuildTask
    {
        // Inputs
        public string PolicyJsonPath { get; set; } = string.Empty;

        // Context
        public string WorldId { get; set; } = "Unknown";
        public string VariantId { get; set; } = "Unknown";
        public string BranchId { get; set; } = "Unknown";
        public string ModelId { get; set; } = "Unknown";

        // Simulated Inputs for Diagnostics (In real world, read from SARIF or Compiler output)
        // Format: "Id|Severity|FromAssembly|ToAssembly|Symbol"
        [Required]
        public ITaskItem[] Diagnostics { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Starting Architecture Policy Routing...");

            // 1. Initialize Policy Engine
            // Basic mocked workflow setup or load from JSON (simplified here)
            // Ideally: Load json from PolicyJsonPath
            var engine = CreateEngine();

            bool failed = false;

            foreach (var item in Diagnostics)
            {
                var diagString = item.ItemSpec;
                var parts = diagString.Split('|');
                if (parts.Length < 5) continue;

                var id = parts[0];
                var severity = parts[1]; // Error, Warning, Info
                var fromAssm = parts[2];
                var toAssm = parts[3];
                var symbol = parts[4];

                var fact = new DiagnosticFact
                {
                    DiagnosticId = id,
                    FromAssembly = fromAssm,
                    ToAssembly = toAssm,
                    Symbol = symbol,
                    WorldId = WorldId,
                    VariantId = VariantId,
                    BranchId = BranchId,
                    ModelId = ModelId
                };

                var result = engine.Evaluate(fact);

                // Enforce Constitutional Downgrade Refusal
                if (IsConstitutional(id))
                {
                    if (severity == "Error" && result.Decision != PolicyDecision.Error)
                    {
                        Log.LogError($"CRITICAL: Attempted to downgrade Constitutional Rule {id}. This is forbidden.");
                        failed = true;
                    }
                    continue; // Constitutional rules are handled by analyzers mostly, but we override if needed?
                    // Actually, if analyzer emitted Error, and result says "Allow", that's a downgrade if we suppress it.
                    // For now, let's just log.
                }

                // Apply Policy Decision
                if (result.Decision == PolicyDecision.Error)
                {
                    Log.LogError($"Policy Violation ({id}): {result.MessageOverride} [Context: {VariantId}/{BranchId}]");
                    failed = true;
                }
                else if (result.Decision == PolicyDecision.Warn)
                {
                    Log.LogWarning($"Policy Warning ({id}): {result.MessageOverride}");
                }
            }

            return !failed;
        }

        private ArchitecturePolicyEngine CreateEngine()
        {
            // For MVP, we return a hardcoded engine or a simple one.
            // In real impl, parse JSON.
            // Here we mimic the test case logic.
            // Let's just create an empty engine for now as we don't have JSON parser yet.
            // Or better, let's allow injecting rules via property for testing?

            // Just return empty engine, defaulting to Allow unless mocked.
            // Real implementation requires JSON deserialization which is a bigger chunk.
            return new ArchitecturePolicyEngine(new RulesEngine.Models.Workflow[0]);
        }

        private bool IsConstitutional(string id)
        {
            // Simple check based on ID range or hardcoded list
            return id.StartsWith("FW1") || id.StartsWith("FW3");
        }
    }
}
