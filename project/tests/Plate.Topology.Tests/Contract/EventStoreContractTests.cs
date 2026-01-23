using Plate.Topology.Contracts.Events;
using Plate.Topology.Contracts.Identity;
using Xunit;
using System.Reflection;

namespace Plate.Topology.Tests.Contract;

/// <summary>
/// Contract tests for ITopologyEventStore per FR-001, FR-012, FR-014.
///
/// Tests verify that the event store contract satisfies requirements for:
/// - Append and read operations for topology events
/// - Deterministic replay by Sequence ordering
/// - Stream isolation by TruthStreamIdentity
/// - Minimal but sufficient API for replay
/// - No storage implementation dependencies
/// </summary>
public class EventStoreContractTests
{
    /// <summary>
    /// Gets the original 3-parameter AppendAsync method (without AppendOptions).
    /// </summary>
    private static MethodInfo? GetCoreAppendAsyncMethod()
    {
        var interfaceType = typeof(ITopologyEventStore);
        return interfaceType.GetMethods()
            .FirstOrDefault(m =>
                m.Name == "AppendAsync" &&
                m.GetParameters().Length == 3 &&
                m.GetParameters()[0].ParameterType == typeof(TruthStreamIdentity));
    }

    #region Interface Existence

    [Fact]
    public void EventStore_Interface_Exists()
    {
        // Arrange & Act - Try to load the interface type
        var interfaceType = typeof(ITopologyEventStore);

        // Assert - The interface should exist
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsInterface);
    }

    [Fact]
    public void EventStore_Interface_IsInCorrectNamespace()
    {
        // Arrange & Act
        var interfaceType = typeof(ITopologyEventStore);

        // Assert - Should be in Plate.Topology.Contracts.Events namespace
        Assert.Equal("Plate.Topology.Contracts.Events", interfaceType.Namespace);
    }

    #endregion

    #region AppendAsync Signature

    [Fact]
    public void EventStore_AppendAsync_Method_Exists()
    {
        // Act - Get the core AppendAsync method (without AppendOptions)
        var method = GetCoreAppendAsyncMethod();

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task));
    }

    [Fact]
    public void EventStore_AppendAsync_HasCorrectParameters()
    {
        // Act
        var method = GetCoreAppendAsyncMethod();
        var parameters = method?.GetParameters();

        // Assert - Should have exactly 3 parameters
        Assert.NotNull(parameters);
        Assert.Equal(3, parameters.Length);

        // First parameter: TruthStreamIdentity stream
        Assert.Equal("stream", parameters[0].Name);
        Assert.Equal(typeof(TruthStreamIdentity), parameters[0].ParameterType);

        // Second parameter: IEnumerable<IPlateTopologyEvent> events
        Assert.Equal("events", parameters[1].Name);
        Assert.Equal(typeof(IEnumerable<IPlateTopologyEvent>), parameters[1].ParameterType);

        // Third parameter: CancellationToken cancellationToken
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [Fact]
    public void EventStore_AppendAsync_IsAsync()
    {
        // Act
        var method = GetCoreAppendAsyncMethod();

        // Assert - Should return Task (async operation)
        Assert.Equal(typeof(Task), method?.ReturnType);
    }

    [Fact]
    public void EventStore_AppendAsync_NoStorageReferences()
    {
        // Arrange
        var method = GetCoreAppendAsyncMethod();

        // Act - Check all parameters
        var parameters = method?.GetParameters();
        var parameterTypes = parameters?.Select(p => p.ParameterType.Name).ToArray();

        // Assert - No RocksDB or other storage implementation types
        Assert.DoesNotContain("RocksDB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
        Assert.DoesNotContain("DB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
    }

    #endregion

    #region ReadAsync Signature

    [Fact]
    public void EventStore_ReadAsync_Method_Exists()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Get the ReadAsync method
        var method = interfaceType.GetMethod("ReadAsync");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(IAsyncEnumerable<IPlateTopologyEvent>));
    }

    [Fact]
    public void EventStore_ReadAsync_HasCorrectParameters()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act
        var method = interfaceType.GetMethod("ReadAsync");
        var parameters = method?.GetParameters();

        // Assert - Should have exactly 3 parameters
        Assert.NotNull(parameters);
        Assert.Equal(3, parameters.Length);

        // First parameter: TruthStreamIdentity stream
        Assert.Equal("stream", parameters[0].Name);
        Assert.Equal(typeof(TruthStreamIdentity), parameters[0].ParameterType);

        // Second parameter: long fromSequenceInclusive
        Assert.Equal("fromSequenceInclusive", parameters[1].Name);
        Assert.Equal(typeof(long), parameters[1].ParameterType);

        // Third parameter: CancellationToken cancellationToken
        Assert.Equal("cancellationToken", parameters[2].Name);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [Fact]
    public void EventStore_ReadAsync_ReturnsAsyncEnumerable()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act
        var method = interfaceType.GetMethod("ReadAsync");

        // Assert - Should return IAsyncEnumerable<IPlateTopologyEvent>
        Assert.NotNull(method);
        Assert.Equal(typeof(IAsyncEnumerable<IPlateTopologyEvent>), method.ReturnType);
    }

    [Fact]
    public void EventStore_ReadAsync_ParameterFromSequenceInclusive_IsLong()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act
        var method = interfaceType.GetMethod("ReadAsync");
        var parameters = method?.GetParameters();

        // Assert - The fromSequenceInclusive parameter should be long
        Assert.Equal("fromSequenceInclusive", parameters?[1].Name);
        Assert.Equal(typeof(long), parameters?[1].ParameterType);
    }

    [Fact]
    public void EventStore_ReadAsync_NoStorageReferences()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);
        var method = interfaceType.GetMethod("ReadAsync");

        // Act - Check all parameters
        var parameters = method?.GetParameters();
        var parameterTypes = parameters?.Select(p => p.ParameterType.Name).ToArray();

        // Assert - No RocksDB or other storage implementation types
        Assert.DoesNotContain("RocksDB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
        Assert.DoesNotContain("DB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
    }

    #endregion

    #region GetLastSequenceAsync Signature

    [Fact]
    public void EventStore_GetLastSequenceAsync_Method_Exists()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Get the GetLastSequenceAsync method
        var method = interfaceType.GetMethod("GetLastSequenceAsync");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task<long?>));
    }

    [Fact]
    public void EventStore_GetLastSequenceAsync_HasCorrectParameters()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act
        var method = interfaceType.GetMethod("GetLastSequenceAsync");
        var parameters = method?.GetParameters();

        // Assert - Should have exactly 2 parameters
        Assert.NotNull(parameters);
        Assert.Equal(2, parameters.Length);

        // First parameter: TruthStreamIdentity stream
        Assert.Equal("stream", parameters[0].Name);
        Assert.Equal(typeof(TruthStreamIdentity), parameters[0].ParameterType);

        // Second parameter: CancellationToken cancellationToken
        Assert.Equal("cancellationToken", parameters[1].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void EventStore_GetLastSequenceAsync_ReturnsNullableLong()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act
        var method = interfaceType.GetMethod("GetLastSequenceAsync");

        // Assert - Should return Task<long?> (nullable long)
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<long?>), method.ReturnType);
    }

    [Fact]
    public void EventStore_GetLastSequenceAsync_NoStorageReferences()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);
        var method = interfaceType.GetMethod("GetLastSequenceAsync");

        // Act - Check all parameters
        var parameters = method?.GetParameters();
        var parameterTypes = parameters?.Select(p => p.ParameterType.Name).ToArray();

        // Assert - No RocksDB or other storage implementation types
        Assert.DoesNotContain("RocksDB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
        Assert.DoesNotContain("DB", string.Join(" ", parameterTypes ?? Array.Empty<string>()));
    }

    #endregion

    #region Contract Compliance

    [Fact]
    public void EventStore_Contract_UsesTruthStreamIdentityForIsolation()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Check all methods for TruthStreamIdentity parameter
        var methods = interfaceType.GetMethods();

        // Assert - All methods should have a TruthStreamIdentity parameter named "stream"
        foreach (var method in methods)
        {
            var hasStreamParameter = method.GetParameters()
                .Any(p => p.ParameterType == typeof(TruthStreamIdentity) && p.Name == "stream");

            Assert.True(hasStreamParameter,
                $"Method {method.Name} should have a TruthStreamIdentity parameter named 'stream' for stream isolation");
        }
    }

    [Fact]
    public void EventStore_Contract_SequenceOrderingParametersExist()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Check methods for Sequence-related parameters
        var readAsyncMethod = interfaceType.GetMethod("ReadAsync");

        // Assert - ReadAsync should have fromSequenceInclusive parameter for ordering
        var hasFromSequenceParameter = readAsyncMethod?.GetParameters()
            .Any(p => p.Name == "fromSequenceInclusive" && p.ParameterType == typeof(long));

        Assert.True(hasFromSequenceParameter,
            "ReadAsync should have 'fromSequenceInclusive' long parameter for deterministic replay ordering");
    }

    [Fact]
    public void EventStore_Contract_AllMethodsAreAsync()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Get all methods
        var methods = interfaceType.GetMethods();

        // Assert - All methods should return Task or IAsyncEnumerable (async)
        foreach (var method in methods)
        {
            var isAsync = typeof(Task).IsAssignableFrom(method.ReturnType) ||
                          method.ReturnType.IsGenericType &&
                          method.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);

            Assert.True(isAsync,
                $"Method {method.Name} should be async (return Task or IAsyncEnumerable)");
        }
    }

    [Fact]
    public void EventStore_Contract_NoStorageImplementationReferences()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);
        var forbiddenTypes = new[] { "RocksDB", "ModernRocksDB", "DBEngine" };

        // Act - Check all method parameters
        var methods = interfaceType.GetMethods();
        var allParameterNames = methods
            .SelectMany(m => m.GetParameters())
            .Select(p => p.ParameterType.Name)
            .ToArray();

        // Assert - No storage implementation types should appear
        var allParameterTypesString = string.Join(" ", allParameterNames);
        foreach (var forbidden in forbiddenTypes)
        {
            Assert.False(allParameterTypesString.Contains(forbidden),
                $"Interface should not reference storage implementation type: {forbidden}");
        }
    }

    [Fact]
    public void EventStore_Contract_UsesIPlateTopologyEvent()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Check AppendAsync and ReadAsync
        var appendAsyncMethod = GetCoreAppendAsyncMethod();
        var readAsyncMethod = interfaceType.GetMethod("ReadAsync");

        // Assert - Both should use IPlateTopologyEvent (not concrete types)
        var appendEventsParam = appendAsyncMethod?.GetParameters()
            .FirstOrDefault(p => p.Name == "events");
        Assert.Equal(typeof(IEnumerable<IPlateTopologyEvent>), appendEventsParam?.ParameterType);

        var readReturnType = readAsyncMethod?.ReturnType;
        Assert.Equal(typeof(IAsyncEnumerable<IPlateTopologyEvent>), readReturnType);
    }

    [Fact]
    public void EventStore_Contract_MinimalSufficientForReplay()
    {
        // Arrange - Requirements for deterministic replay per FR-001, FR-012, FR-014
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Check for essential methods
        var hasAppend = GetCoreAppendAsyncMethod() != null;
        var hasRead = interfaceType.GetMethod("ReadAsync") != null;
        var hasGetLastSequence = interfaceType.GetMethod("GetLastSequenceAsync") != null;

        // Assert - Should have append, read, and optional sequence query
        Assert.True(hasAppend, "Contract should have AppendAsync for writing events");
        Assert.True(hasRead, "Contract should have ReadAsync for replay");
        Assert.True(hasGetLastSequence, "Contract should have GetLastSequenceAsync for version tracking");
    }

    [Fact]
    public void EventStore_Contract_AllMethodsHaveCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(ITopologyEventStore);

        // Act - Get all methods
        var methods = interfaceType.GetMethods();

        // Assert - All methods should have CancellationToken parameter
        foreach (var method in methods)
        {
            var hasCancellationToken = method.GetParameters()
                .Any(p => p.ParameterType == typeof(CancellationToken) && p.Name == "cancellationToken");

            Assert.True(hasCancellationToken,
                $"Method {method.Name} should have a CancellationToken parameter");
        }
    }

    #endregion
}
