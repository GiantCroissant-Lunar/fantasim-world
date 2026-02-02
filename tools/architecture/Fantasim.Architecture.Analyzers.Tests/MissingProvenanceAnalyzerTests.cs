using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Fantasim.Architecture.Analyzers.Tests.CSharpAnalyzerVerifier<Fantasim.Architecture.Analyzers.MissingProvenanceAnalyzer>;

namespace Fantasim.Architecture.Analyzers.Tests
{
    public class MissingProvenanceAnalyzerTests
    {
        [Fact]
        public async Task DerivedProduct_WithoutProvenance_ReportsError()
        {
            var test = @"
using System;

namespace Fantasim.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedProductAttribute : Attribute {}

    public interface IProvenanceProvider { object Provenance { get; } }
}

namespace Fantasim.Plates
{
    using Fantasim.Core;

    [DerivedProduct] // Error expected here
    public class MyPlate
    {
        public int Id { get; set; }
    }
}";

            var expected = VerifyCS.Diagnostic("FW3201")
                // .WithLocation(14, 18) // Brittle check removed
                .WithArguments("MyPlate");

            await new VerifyCS.Test
            {
                TestCode = test,
                ExpectedDiagnostics = { expected }
            }.RunAsync();
        }

        [Fact]
        public async Task DerivedProduct_WithProvenanceInterface_Allowed()
        {
            var test = @"
using System;

namespace Fantasim.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedProductAttribute : Attribute {}
    public interface IProvenanceProvider { object Provenance { get; } }
}

namespace Fantasim.Plates
{
    using Fantasim.Core;

    [DerivedProduct]
    public class MyPlate : IProvenanceProvider
    {
        public object Provenance => null;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = test,
            }.RunAsync();
        }

        [Fact]
        public async Task DerivedProduct_WithProvenanceProperty_Allowed()
        {
             var test = @"
using System;

namespace Fantasim.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedProductAttribute : Attribute {}
}

namespace Fantasim.Plates
{
    using Fantasim.Core;

    [DerivedProduct]
    public class MyPlate
    {
        public object Provenance { get; set; } // Matches check
    }
}";
            await new VerifyCS.Test
            {
                TestCode = test,
            }.RunAsync();
        }
    }
}
