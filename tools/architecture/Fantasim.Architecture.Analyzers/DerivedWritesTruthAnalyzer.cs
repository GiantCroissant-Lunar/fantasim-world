using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fantasim.Architecture.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DerivedWritesTruthAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.DerivedWritesTruth,
            "Derived code must not mutate truth",
            "Derived assembly '{0}' calls Truth-mutating API '{1}'",
            "Architecture",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Derived layers must be read-only with respect to Truth EventStore.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var layer = LayerInference.InferLayer(compilationContext.Compilation, compilationContext.Options);

                // Only enforce if we are in a Derived layer
                if (layer != ArchitectureLayer.Derived)
                    return;

                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // We need to resolve the symbol being invoked
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                return;

            // Check if the method is a "Truth Writer"
            // Examples: IEventStore.Append, TruthCommandBus.Send
            // We check by checking the containing type and method name.

            if (IsTruthMutator(methodSymbol))
            {
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(),
                    context.Compilation.AssemblyName,
                    $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}");

                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsTruthMutator(IMethodSymbol method)
        {
            // Allowlist approach or Denylist?
            // "IEventStore.Append" is explicit in RFC.
            // "TruthCommandBus.Send" is explicit in RFC.

            var typeName = method.ContainingType.Name;
            var fullTypeName = method.ContainingType.ToDisplayString(); // e.g. Fantasim.M1.Truth.IEventStore

            if (typeName == "IEventStore" && method.Name == "Append")
                return true;

            if (typeName == "TruthCommandBus" && method.Name == "Send")
                return true;

            // Also check for [TruthMutator] attribute if we want to be fancy later.
            return false;
        }
    }
}
