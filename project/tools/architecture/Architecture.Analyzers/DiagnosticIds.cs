namespace Fantasim.Architecture.Analyzers
{
    public static class DiagnosticIds
    {
        public const string TruthReferenceDerived = "FW1001";
        public const string DerivedWritesTruth = "FW1002";
        public const string DerivedPersistedAsTruth = "FW1003";
        public const string TruthReferenceDerivedProduct = "FW1004";

        public const string PolicyHashIncomplete = "FW3101";
        public const string MissingProvenance = "FW3201";

        public const string VariantForbiddenEdge = "FW4201";
    }
}
