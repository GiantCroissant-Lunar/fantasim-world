using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fantasim.Architecture.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PolicyHashIncompleteAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.PolicyHashIncomplete,
            "Policy/Cache identity builders must include PolicyHash",
            "Method '{0}' builds identity but misses PolicyHash",
            "Architecture",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "If PolicyHash is present in the context, it must be included in cache identity construction.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Placeholder: Not fully implemented yet as it requires defining ICacheIdentityBuilder pattern.
            // context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }
    }
}
