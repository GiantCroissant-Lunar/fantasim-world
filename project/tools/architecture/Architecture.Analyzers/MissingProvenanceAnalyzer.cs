using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fantasim.Architecture.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingProvenanceAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.MissingProvenance,
            "Derived products must attach provenance",
            "Type '{0}' is marked [DerivedProduct] but does not implement IProvenanceProvider",
            "Architecture",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All types marked [DerivedProduct] must attach Provenance metadata via IProvenanceProvider.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            // Check for [DerivedProduct] attribute
            // We search by name allowing for loose matching or exact if possible.
            // Ideally "Fantasim.Core.DerivedProductAttribute"
            if (!HasAttribute(namedType, "DerivedProductAttribute"))
                return;

            // Check if implements IProvenanceProvider
            // Ideally "Fantasim.Core.IProvenanceProvider"
            bool implementsProvenance = namedType.AllInterfaces.Any(i => i.Name == "IProvenanceProvider");

            if (!implementsProvenance)
            {
                // Also check for a property named "Provenance" as a fallback/convention?
                // RFC says "must attach Provenance metadata".
                // Let's be strict: require the interface OR the property.
                if (!namedType.GetMembers("Provenance").Any())
                {
                     var diagnostic = Diagnostic.Create(Rule, namedType.Locations[0], namedType.Name);
                     context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private bool HasAttribute(INamedTypeSymbol symbol, string attributeName)
        {
            return symbol.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Name == attributeName);
        }
    }
}
