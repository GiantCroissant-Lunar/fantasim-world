using System.Linq;
using RulesEngine.Models;
using System.Collections.Generic;

namespace Fantasim.Architecture.Policy
{
    public class ArchitecturePolicyEngine
    {
        private readonly RulesEngine.RulesEngine _engine;
        private readonly bool _strictMode;

        public ArchitecturePolicyEngine(Workflow[] workflows, bool strictMode = false)
        {
            _engine = new RulesEngine.RulesEngine(workflows, null);
            _strictMode = strictMode;
        }

        public PolicyResult Evaluate(DiagnosticFact fact)
        {
            // Constraint: RulesEngine cannot invent diagnostics.
            // It only evaluates the Fact provided.

            var resultList = _engine.ExecuteAllRulesAsync("ArchitectureGovernance", fact).Result;

            // Simple aggregation logic:
            // 1. If any rule returns Error, result is Error.
            // 2. If any rule returns Warn, result is Warn.
            // 3. If any rule returns Suppress, and it's NOT constitutional, result is Suppress.

            // For now, let's look for the "highest severity" output.

            var failure = resultList.FirstOrDefault(r => !r.IsSuccess);

            if (failure != null)
            {
                // In RulesEngine, !IsSuccess usually triggers the "Failure" message.
                // We can use CustomErrorMessage or ActionResult to pass back decision.

                // Convention: If rule fails, it means "Forbidden".
                // We need to extract severity from rule config or result action.

                return new PolicyResult
                {
                    Decision = PolicyDecision.Error,
                    MessageOverride = failure.ExceptionMessage ?? failure.Rule.ErrorMessage
                };
            }

            return new PolicyResult { Decision = PolicyDecision.Allow };
        }
    }
}
