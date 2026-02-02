using System.Collections.Generic;
using RulesEngine.Models;
using Xunit;

namespace Fantasim.Architecture.Policy.Tests
{
    public class PolicyEvaluationTests
    {
        [Fact]
        public void ForbidNamespaceRule_ReturnsError_WhenViolated()
        {
            // Define a simple workflow programmatically for testing
            var workflow = new Workflow
            {
                WorkflowName = "ArchitectureGovernance",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "ForbidManaInWuxia",
                        ErrorMessage = "Wuxia variant forbids Mana modules",
                        // ErrorType = ErrorType.Error, // Not standard property
                        // Rule Expression: Error if Variant is Wuxia AND Namespace contains Mana
                        Expression = "Context.VariantId == \"wuxia\" && Context.ToAssembly.Contains(\"Mana\")"
                        // Note: RulesEngine uses "Context" for the input object by default if not renamed?
                        // Actually, by default it maps the input params.
                        // We will pass DiagnosticFact as the input.
                        // Let's assume input is named "input1" or we use "this"?
                        // Standard RulesEngine: input1.Property
                        // Let's use flexible expression: "VariantId == \"wuxia\" && ToAssembly.Contains(\"Mana\")"
                    }
                }
            };

            // Re-define rule expression to match RulesEngine default lambda syntax
            workflow.Rules = new List<Rule>(workflow.Rules); // Ensure list
            ((List<Rule>)workflow.Rules)[0].Expression = "VariantId == \"wuxia\" && ToAssembly.Contains(\"Mana\")";

            var engine = new ArchitecturePolicyEngine(new[] { workflow });

            var fact = new DiagnosticFact
            {
                VariantId = "wuxia",
                ToAssembly = "Fantasim.Mana.Core",
                DiagnosticId = "FW2001"
            };

            var result = engine.Evaluate(fact);

            // Expect Error because rule evaluated to True (which means Condition Met -> Action Triggered?)
            // Wait, RulesEngine logic:
            // "Expression" evaluates to boolean.
            // If True -> Rule Success -> SuccessEvent.
            // If False -> Rule Fail -> ErrorMessage.

            // This is inverted for "Governance".
            // Typically: "Expression" defines "Consistency".
            // If Expression is true, it's VALID.
            // If Expression is false, it's INVALID (Error).

            // So rewording the rule:
            // Rule: "AllowWuxiaDeps"
            // Expression: "!(VariantId == \"wuxia\" && ToAssembly.Contains(\"Mana\"))"
            // If True (Not Wuxia/Mana) -> OK.
            // If False (Is Wuxia+Mana) -> Error.

            ((List<Rule>)workflow.Rules)[0].Expression = "!(VariantId == \"wuxia\" && ToAssembly.Contains(\"Mana\"))";

            // Re-init engine with corrected rule
            engine = new ArchitecturePolicyEngine(new[] { workflow });

            result = engine.Evaluate(fact);

            Assert.Equal(PolicyDecision.Error, result.Decision);
            Assert.Equal("Wuxia variant forbids Mana modules", result.MessageOverride);
        }

        [Fact]
        public void AllowedDependency_ReturnsAllow()
        {
            var workflow = new Workflow
            {
                WorkflowName = "ArchitectureGovernance",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        RuleName = "ForbidManaInWuxia",
                        ErrorMessage = "Wuxia variant forbids Mana modules",
                        Expression = "!(VariantId == \"wuxia\" && ToAssembly.Contains(\"Mana\"))"
                    }
                }
            };

            var engine = new ArchitecturePolicyEngine(new[] { workflow });

            var fact = new DiagnosticFact
            {
                VariantId = "wuxia",
                ToAssembly = "Fantasim.Physical.Core", // Not Mana
                DiagnosticId = "FW2001"
            };

            var result = engine.Evaluate(fact);

            Assert.Equal(PolicyDecision.Allow, result.Decision);
        }
    }
}
