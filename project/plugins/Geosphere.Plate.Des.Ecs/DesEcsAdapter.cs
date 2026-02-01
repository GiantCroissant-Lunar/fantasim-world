using System;
using System.Collections.Generic;
using Plate.TimeDete.Time.Primitives;
using UnifyEcs;
using UnifySerialization.Abstractions;

namespace FantaSim.Geosphere.Plate.Des.Ecs;

/// <summary>
/// ECS adapter for deterministic execution system (DES).
/// 
/// Per RFC-V2-0015, provides bridge between legacy DES and new ECS runtime:
/// - Registers drivers and triggers with monotonic handles
/// - Schedules work items with RFC-compliant ordering
/// - Executes work deterministically via ECS system
/// - Buffers events to truth event sink
/// </summary>
public sealed class DesEcsAdapter : IDisposable
{
    private readonly IEcsExecutionContext _context;
    private readonly DesExecutionSystem _executionSystem;
    private readonly World _ecsWorld;
    private readonly Dictionary<object, Entity> _driverToEntity;
    private readonly Dictionary<object, Entity> _triggerToEntity;
    private ulong _nextTieBreak;

    /// <summary>
    /// Initializes a new DesEcsAdapter instance.
    /// </summary>
    /// <param name="context">The ECS execution context.</param>
    /// <param name="ecsWorld">The ECS world.</param>
    public DesEcsAdapter(IEcsExecutionContext context, World ecsWorld)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _ecsWorld = ecsWorld ?? throw new ArgumentNullException(nameof(ecsWorld));
        _executionSystem = new DesExecutionSystem(context, ecsWorld);
        _driverToEntity = new Dictionary<object, Entity>();
        _triggerToEntity = new Dictionary<object, Entity>();
        _nextTieBreak = 0UL;

        // Register system with ECS world
        ecsWorld.AddSystem(_executionSystem, ExecutionPhase.Update);
    }

    /// <summary>
    /// Registers a driver instance for ECS execution.
    /// </summary>
    /// <param name="driver">The driver to register.</param>
    /// <param name="sphere">The sphere this driver belongs to.</param>
    /// <param name="kind">The work kind this driver performs.</param>
    /// <returns>The monotonic driver handle.</returns>
    public DriverHandle RegisterDriver(IDriver driver, SphereId sphere, WorkKind kind)
    {
        if (driver == null)
            throw new ArgumentNullException(nameof(driver));

        // Register with context
        var handle = _context.DriverRegistry.Register(driver);

        // Create ECS entity for this driver
        var entity = _ecsWorld.CreateEntity();
        entity.AddComponent(new DesWorkItemComponent
        {
            When = CanonicalTick.Zero,  // Will be set when scheduled
            Sphere = sphere,
            Kind = kind,
            TieBreak = 0UL,  // Will be set when scheduled
            DriverHandle = handle,
            TriggerHandle = TriggerHandle.Empty
        });

        _driverToEntity[driver] = entity;
        return handle;
    }

    /// <summary>
    /// Registers a trigger instance for ECS execution.
    /// </summary>
    /// <param name="trigger">The trigger to register.</param>
    /// <param name="sphere">The sphere this trigger belongs to.</param>
    /// <param name="kind">The work kind this trigger performs.</param>
    /// <returns>The monotonic trigger handle.</returns>
    public TriggerHandle RegisterTrigger(ITrigger trigger, SphereId sphere, WorkKind kind)
    {
        if (trigger == null)
            throw new ArgumentNullException(nameof(trigger));

        // Register with context
        var handle = _context.TriggerRegistry.Register(trigger);

        // Create ECS entity for this trigger
        var entity = _ecsWorld.CreateEntity();
        entity.AddComponent(new DesWorkItemComponent
        {
            When = CanonicalTick.Zero,  // Will be set when scheduled
            Sphere = sphere,
            Kind = kind,
            TieBreak = 0UL,  // Will be set when scheduled
            DriverHandle = DriverHandle.Empty,
            TriggerHandle = handle
        });

        _triggerToEntity[trigger] = entity;
        return handle;
    }

    /// <summary>
    /// Schedules a driver for execution at a specific tick.
    /// </summary>
    /// <param name="driver">The driver to schedule.</param>
    /// <param name="when">When to execute the driver.</param>
    public void ScheduleDriver(IDriver driver, CanonicalTick when)
    {
        if (!_driverToEntity.TryGetValue(driver, out var entity))
            throw new KeyNotFoundException("Driver is not registered");

        var component = entity.GetComponent<DesWorkItemComponent>();
        component.When = when;
        component.TieBreak = _nextTieBreak++;

        entity.SetComponent(component);
    }

    /// <summary>
    /// Schedules a trigger for execution at a specific tick.
    /// </summary>
    /// <param name="trigger">The trigger to schedule.</param>
    /// <param name="when">When to execute the trigger.</param>
    public void ScheduleTrigger(ITrigger trigger, CanonicalTick when)
    {
        if (!_triggerToEntity.TryGetValue(trigger, out var entity))
            throw new KeyNotFoundException("Trigger is not registered");

        var component = entity.GetComponent<DesWorkItemComponent>();
        component.When = when;
        component.TieBreak = _nextTieBreak++;

        entity.SetComponent(component);
    }

    /// <summary>
    /// Executes all scheduled work for the current tick.
    /// </summary>
    /// <param name="tick">The current canonical tick.</param>
    public void ExecuteTick(CanonicalTick tick)
    {
        _context.BeginTick(tick);
        
        // ECS world update will execute DesExecutionSystem
        // which processes all DesWorkItemComponents for the current tick
        _ecsWorld.Update(0f);
        
        _context.EndTick();
    }

    /// <summary>
    /// Gets committed truth event drafts from the current tick.
    /// </summary>
    /// <returns>Read-only list of committed drafts.</returns>
    public IReadOnlyList<ITruthEventDraft> GetCommittedDrafts()
    {
        return _context.GetCommittedDrafts();
    }

    /// <summary>
    /// Disposes the adapter, cleaning up ECS entities and systems.
    /// </summary>
    public void Dispose()
    {
        _ecsWorld.RemoveSystem(_executionSystem);
        
        foreach (var entity in _driverToEntity.Values)
        {
            entity.Destroy();
        }
        
        foreach (var entity in _triggerToEntity.Values)
        {
            entity.Destroy();
        }
        
        _driverToEntity.Clear();
        _triggerToEntity.Clear();
    }
}

/// <summary>
/// Interface for DES drivers.
/// </summary>
public interface IDriver
{
}

/// <summary>
/// Interface for DES triggers.
/// </summary>
public interface ITrigger
{
}