using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Fantasim.Architecture.Analyzers.Tests.CSharpAnalyzerVerifier<Fantasim.Architecture.Analyzers.DerivedWritesTruthAnalyzer>;

namespace Fantasim.Architecture.Analyzers.Tests
{
    public class DerivedWritesTruthAnalyzerTests
    {
        [Fact]
        public async Task DerivedCallingTruthMutator_ReportsError()
        {
            var test = @"
using Fantasim.World.Truth;

namespace Fantasim.Plates.Derived
{
    public class MyDerivedService
    {
        public void DoSomething(IEventStore store, TruthCommandBus bus)
        {
            store.Append(""event"");
            bus.Send(""command"");
        }
    }
}

namespace Fantasim.World.Truth
{
    public interface IEventStore { void Append(string e); }
    public class TruthCommandBus { public void Send(string c) {} }
}
";

            // We expect errors because the assembly is simulated as "Fantasim.Plates.Derived"
            var expected1 = VerifyCS.Diagnostic("FW1002")
                .WithLocation(10, 13)
                .WithArguments("Fantasim.Plates.Derived", "IEventStore.Append");

            var expected2 = VerifyCS.Diagnostic("FW1002")
                .WithLocation(11, 13)
                .WithArguments("Fantasim.Plates.Derived", "TruthCommandBus.Send");

            await new VerifyCS.Test
            {
                TestCode = test,
                SolutionTransforms = {
                    (solution, projectId) => {
                        return solution.WithProjectAssemblyName(projectId, "Fantasim.Plates.Derived");
                    }
                },
                ExpectedDiagnostics = { expected1, expected2 }
            }.RunAsync();
        }

        [Fact]
        public async Task TruthCallingTruthMutator_Allowed()
        {
             var test = @"
using Fantasim.World.Truth;

namespace Fantasim.World.Truth
{
    public class MyTruthService
    {
        public void DoSomething(IEventStore store)
        {
            store.Append(""event""); // OK
        }
    }

    public interface IEventStore { void Append(string e); }
}
";
            // No diagnostics expected
            await new VerifyCS.Test
            {
                TestCode = test,
                SolutionTransforms = {
                    (solution, projectId) => {
                        return solution.WithProjectAssemblyName(projectId, "Fantasim.World.Truth");
                    }
                }
            }.RunAsync();
        }
    }
}
