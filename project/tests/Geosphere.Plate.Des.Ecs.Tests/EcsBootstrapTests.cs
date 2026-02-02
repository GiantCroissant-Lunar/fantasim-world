using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Plate.TimeDete.Time.Primitives;
using UnifyEcs;
using FantaSim.Geosphere.Plate.Des.Ecs;
using FantaSim.Geosphere.Plate.Topology.Contracts;

namespace FantaSim.Geosphere.Plate.Des.Ecs.Tests;

/// <summary>
/// Bootstrap tests for ECS execution system verifying:
/// - RFC-V2-0015 compliant ordering (When → Sphere → Kind → TieBreak)
/// - ALC-safe registries (handles allocated before driver/trigger use)
/// - Deterministic execution across multiple ticks
/// </summary>
[TestFixture]
public sealed class EcsBootstrapTests
{
    private readonly World _ecsWorld;
    private readonly IEcsExecutionContext _context;
    private readonly DesEcsAdapter _adapter;
    private readonly List<MockExecutableDriver> _drivers;
    private readonly List<MockExecutableTrigger> _triggers;
    private readonly List<ExecutionRecord> _executionLog;

    [SetUp]
    public void SetUp()
    {
        _ecsWorld = new World();
        _context = new ArchExecutionContext();
        _adapter = new DesEcsAdapter(_context, _ecsWorld);
        _drivers = new List<MockExecutableDriver>();
        _triggers = new List<MockExecutableTrigger>();
        _executionLog = new List<ExecutionRecord>();
    }

    [TearDown]
    public void TearDown()
    {
        _adapter?.Dispose();
        _context?.Dispose();
        _ecsWorld?.Dispose();
    }

    /// <summary>
    /// Test: RFC-compliant ordering (When → Sphere → Kind → TieBreak).
    /// Verifies work items execute in correct order based on RFC-V2-0015.
    /// </summary>
    [Test]
    public void RfcCompliantOrdering_WhenSphereKindTieBreak()
    {
        // Arrange: Register drivers with different ordering keys
        var driver1 = CreateDriver("Driver1", SphereId.Crust, WorkKind.BoundaryUpdate);
        var driver2 = CreateDriver("Driver2", SphereId.Core, WorkKind.BoundaryUpdate);
        var driver3 = CreateDriver("Driver3", SphereId.Crust, WorkKind.BoundaryCreation);
        var driver4 = CreateDriver("Driver4", SphereId.Crust, WorkKind.BoundaryUpdate);

        var tick = CanonicalTick.From(100);

        // Schedule work items out of order to test sorting
        _adapter.ScheduleDriver(driver4, tick);  // TieBreak 0
        _adapter.ScheduleDriver(driver2, tick);  // TieBreak 1
        _adapter.ScheduleDriver(driver3, tick);  // TieBreak 2
        _adapter.ScheduleDriver(driver1, tick);  // TieBreak 3

        // Act: Execute tick
        _adapter.ExecuteTick(tick);

        // Assert: Execution order is RFC-compliant
        // Expected order:
        // 1. Core sphere before Crust sphere (driver2, then ...)
        // 2. Within same sphere: Kind order (BoundaryCreation before BoundaryUpdate)
        // 3. Within same kind: TieBreak order
        var expectedOrder = new[]
        {
            "Driver2",  // Core sphere, BoundaryUpdate
            "Driver3",  // Crust sphere, BoundaryCreation (before Update)
            "Driver4",  // Crust sphere, BoundaryUpdate, TieBreak 0
            "Driver1"   // Crust sphere, BoundaryUpdate, TieBreak 3
        };

        Assert.That(_executionLog.Select(r => r.Name), Is.EqualTo(expectedOrder),
            "Execution order must be RFC-compliant: When → Sphere → Kind → TieBreak");
    }

    /// <summary>
    /// Test: ALC-safe handle allocation (allocated before first use).
    /// Verifies handles are monotonic and never reused.
    /// </summary>
    [Test]
    public void AlcSafeHandles_MonotonicAndNoReuse()
    {
        // Arrange: Register multiple drivers
        var driver1 = CreateDriver("Driver1", SphereId.Crust, WorkKind.BoundaryUpdate);
        var driver2 = CreateDriver("Driver2", SphereId.Crust, WorkKind.BoundaryUpdate);
        var driver3 = CreateDriver("Driver3", SphereId.Crust, WorkKind.BoundaryUpdate);

        // Register multiple triggers
        var trigger1 = CreateTrigger("Trigger1", SphereId.Crust, WorkKind.JunctionUpdate);
        var trigger2 = CreateTrigger("Trigger2", SphereId.Crust, WorkKind.JunctionUpdate);

        // Act: Get handles
        var driver1Handle = _context.DriverRegistry.GetHandles<MockExecutableDriver>();
        var driver2Handle = _context.DriverRegistry.GetHandles<MockExecutableDriver>();
        var trigger1Handle = _context.TriggerRegistry.GetHandles<MockExecutableTrigger>();

        // Assert: Handles are monotonically increasing
        var driverHandles = _context.DriverRegistry.GetHandles<MockExecutableDriver>();
        var driverValues = driverHandles.Select(h => h.Value).OrderBy(v => v).ToArray();
        for (int i = 1; i < driverValues.Length; i++)
        {
            Assert.That(driverValues[i], Is.GreaterThan(driverValues[i - 1]),
                "Driver handles must be monotonically increasing");
        }

        var triggerHandles = _context.TriggerRegistry.GetHandles<MockExecutableTrigger>();
        var triggerValues = triggerHandles.Select(h => h.Value).OrderBy(v => v).ToArray();
        for (int i = 1; i < triggerValues.Length; i++)
        {
            Assert.That(triggerValues[i], Is.GreaterThan(triggerValues[i - 1]),
                "Trigger handles must be monotonically increasing");
        }

        // Assert: No handle reuse after disposal
        var oldDriverHandleValue = driverValues.Last();
        _context.ClearDrafts();

        // Re-register should allocate new handles, not reuse
        var driver4 = CreateDriver("Driver4", SphereId.Crust, WorkKind.BoundaryUpdate);
        var newDriverHandles = _context.DriverRegistry.GetHandles<MockExecutableDriver>();
        var newDriverValues = newDriverHandles.Select(h => h.Value).OrderBy(v => v).ToArray();

        Assert.That(newDriverValues, Has.Length.EqualTo(driverValues.Length + 1),
            "New driver should allocate new handle, not reuse old handle");

        var newDriverHandleValue = newDriverValues.Max();
        Assert.That(newDriverHandleValue, Is.GreaterThan(oldDriverHandleValue),
            "New handle must be greater than old handle (no reuse)");
    }

    /// <summary>
    /// Test: Deterministic execution across multiple ticks.
    /// Verifies execution is reproducible across multiple simulation ticks.
    /// </summary>
    [Test]
    public void DeterministicExecution_MultipleTicks()
    {
        // Arrange: Register drivers
        var driver1 = CreateDriver("Driver1", SphereId.Crust, WorkKind.BoundaryUpdate);
        var driver2 = CreateDriver("Driver2", SphereId.Crust, WorkKind.BoundaryCreation);

        // Schedule work items for multiple ticks
        _adapter.ScheduleDriver(driver1, CanonicalTick.From(100));
        _adapter.ScheduleDriver(driver2, CanonicalTick.From(100));
        _adapter.ScheduleDriver(driver1, CanonicalTick.From(101));
        _adapter.ScheduleDriver(driver2, CanonicalTick.From(101));

        // Act: Execute ticks
        _adapter.ExecuteTick(CanonicalTick.From(100));
        var tick100Execution = _executionLog.ToList();
        _executionLog.Clear();

        _adapter.ExecuteTick(CanonicalTick.From(101));
        var tick101Execution = _executionLog.ToList();

        // Assert: Each tick executes only its scheduled work
        Assert.That(tick100Execution, Has.Count.EqualTo(2),
            "Tick 100 should execute 2 work items");
        Assert.That(tick100Execution.All(r => r.Tick == CanonicalTick.From(100)),
            "Tick 100 should only execute work items scheduled for tick 100");

        Assert.That(tick101Execution, Has.Count.EqualTo(2),
            "Tick 101 should execute 2 work items");
        Assert.That(tick101Execution.All(r => r.Tick == CanonicalTick.From(101)),
            "Tick 101 should only execute work items scheduled for tick 101");

        // Assert: Execution order is deterministic within each tick
        Assert.That(tick100Execution.Select(r => r.Name), Is.EqualTo(new[] { "Driver2", "Driver1" }),
            "Tick 100 execution order must be deterministic");

        Assert.That(tick101Execution.Select(r => r.Name), Is.EqualTo(new[] { "Driver2", "Driver1" }),
            "Tick 101 execution order must be deterministic and match tick 100");
    }

    /// <summary>
    /// Test: Event draft buffering and commit.
    /// Verifies event drafts are buffered per-tick and committed on tick end.
    /// </summary>
    [Test]
    public void EventDraftBuffering_PerTickCommit()
    {
        // Arrange: Register drivers
        var driver1 = CreateDriver("Driver1", SphereId.Crust, WorkKind.BoundaryUpdate);
        var driver2 = CreateDriver("Driver2", SphereId.Crust, WorkKind.BoundaryCreation);

        var tick = CanonicalTick.From(100);

        // Act: Execute tick
        _adapter.ExecuteTick(tick);

        // Assert: Drafts are committed on tick end
        var drafts = _adapter.GetCommittedDrafts();
        Assert.That(drafts, Has.Count.EqualTo(2),
            "All executed work items should produce event drafts");

        // Assert: Drafts are cleared on next tick begin
        _adapter.ExecuteTick(CanonicalTick.From(101));
        var newDrafts = _adapter.GetCommittedDrafts();
        Assert.That(newDrafts, Has.Count.EqualTo(0),
            "Drafts should be cleared on next tick begin (no work scheduled)");
    }

    #region Helper Methods

    private MockExecutableDriver CreateDriver(string name, SphereId sphere, WorkKind kind)
    {
        var driver = new MockExecutableDriver(name, _executionLog);
        _drivers.Add(driver);
        _adapter.RegisterDriver(driver, sphere, kind);
        return driver;
    }

    private MockExecutableTrigger CreateTrigger(string name, SphereId sphere, WorkKind kind)
    {
        var trigger = new MockExecutableTrigger(name, _executionLog);
        _triggers.Add(trigger);
        _adapter.RegisterTrigger(trigger, sphere, kind);
        return trigger;
    }

    #endregion

    #region Mock Classes

    private sealed class MockExecutableDriver : IExecutableDriver
    {
        private readonly string _name;
        private readonly List<ExecutionRecord> _log;

        public MockExecutableDriver(string name, List<ExecutionRecord> log)
        {
            _name = name;
            _log = log;
        }

        public void Execute(CanonicalTick tick)
        {
            _log.Add(new ExecutionRecord(_name, tick));
        }
    }

    private sealed class MockExecutableTrigger : IExecutableTrigger
    {
        private readonly string _name;
        private readonly List<ExecutionRecord> _log;

        public MockExecutableTrigger(string name, List<ExecutionRecord> log)
        {
            _name = name;
            _log = log;
        }

        public void Execute(CanonicalTick tick)
        {
            _log.Add(new ExecutionRecord(_name, tick));
        }
    }

    private sealed record ExecutionRecord(string Name, CanonicalTick Tick);

    #endregion
}
