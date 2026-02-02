using System.Text.Json.Serialization;

namespace Fantasim.Architecture.Policy
{
    public class DiagnosticFact
    {
        public string DiagnosticId { get; set; } = string.Empty;
        public string RuleKind { get; set; } = string.Empty;
        public string FromAssembly { get; set; } = string.Empty;
        public string ToAssembly { get; set; } = string.Empty;
        public string FromLayer { get; set; } = string.Empty; // Truth / Derived
        public string ToLayer { get; set; } = string.Empty;

        public string WorldId { get; set; } = "Unknown";
        public string VariantId { get; set; } = "Unknown";
        public string BranchId { get; set; } = "Unknown";
        public string ModelId { get; set; } = "Unknown";

        public string Symbol { get; set; } = string.Empty;
    }
}
