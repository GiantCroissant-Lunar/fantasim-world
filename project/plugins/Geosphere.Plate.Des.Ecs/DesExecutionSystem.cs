using System;
using System.Collections.Generic;
using System.Linq;
using Plate.TimeDete.Time.Primitives;
using UnifyEcs;

namespace FantaSim.Geosphere.Plate.Des.Ecs;

/// <summary>
/// ECS system for deterministic execution of DES work items.
///
/// Per RFC-V2-0015, executes work items in order:
/// 1. When (CanonicalTick) - ascending
/// 2. Sphere (SphereId) - by enum order
/// 3. Kind (WorkKind) - by enum order
/// 4. TieBreak (ulong) - ascending
///
/// Only executes work items scheduled for the current tick.
/// </summary>
public sealed class DesExecutionSystem : ISystem
{
    private readonly IEcsExecutionContext _context;
    private readonly World _ecsWorld;
    private readonly List<Entity> _executionQueue;

    /// <summary>
    /// Initializes a new DesExecutionSystem instance.
    /// </summary>
    /// <param name="context">The ECS execution context.</param>
    /// <param name="ecsWorld">The ECS world.</param>
    public DesExecutionSystem(IEcsExecutionContext context, World ecsWorld)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _ecsWorld = ecsWorld ?? throw new ArgumentNullException(nameof(ecsWorld));
        _executionQueue = new List<Entity>();
    }

    /// <inheritdoc/>
    public void OnUpdate(float deltaTime)
    {
        var currentTick = _context.CurrentTick;

        // Query all entities with DesWorkItemComponent
        var query = _ecsWorld.Query<DesWorkItemComponent>();

        // Filter and sort work items for current tick
        var workItems = query
            .Where(entity =>
            {
                var component = entity.GetComponent<DesWorkItemComponent>();
                return component.When == currentTick;
            })
            .Select(entity =>
            {
                var component = entity.GetComponent<DesWorkItemComponent>();
                return (Entity: entity, Component: component);
            })
            .OrderBy(tuple => tuple.Component.When)
            .ThenBy(tuple => tuple.Component.Sphere)
            .ThenBy(tuple => tuple.Component.Kind)
            .ThenBy(tuple => tuple.Component.TieBreak)
            .ToList();

        // Execute work items in sorted order
        foreach (var (entity, component) in workItems)
        {
            ExecuteWorkItem(entity, component);
        }
    }

    /// <summary>
    /// Executes a single work item by resolving handles and calling driver/trigger.
    /// </summary>
    /// <param name="entity">The entity containing the work item component.</param>
    /// <param name="component">The work item component to execute.</param>
    private void ExecuteWorkItem(Entity entity, DesWorkItemComponent component)
    {
        try
        {
            // Execute driver or trigger based on handles
            if (component.DriverHandle != DriverHandle.Empty)
            {
                var driver = _context.DriverRegistry.Resolve<object>(component.DriverHandle);

                // Execute driver (polymorphic call)
                if (driver is IExecutableDriver executableDriver)
                {
                    executableDriver.Execute(_context.CurrentTick);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Driver type {driver.GetType().Name} does not implement IExecutableDriver");
                }
            }

            if (component.TriggerHandle != TriggerHandle.Empty)
            {
                var trigger = _context.TriggerRegistry.Resolve<object>(component.TriggerHandle);

                // Execute trigger (polymorphic call)
                if (trigger is IExecutableTrigger executableTrigger)
                {
                    executableTrigger.Execute(_context.CurrentTick);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Trigger type {trigger.GetType().Name} does not implement IExecutableTrigger");
                }
            }
        }
        catch (Exception ex)
        {
            // Wrap execution exceptions with context
            throw new WorkItemExecutionException(
                $"Failed to execute work item: {component}",
                component,
                ex);
        }
    }
}

/// <summary>
/// Exception thrown when work item execution fails.
/// </summary>
public sealed class WorkItemExecutionException : InvalidOperationException
{
    /// <summary>
    /// Gets the work item component that failed to execute.
    /// </summary>
    public DesWorkItemComponent WorkItem { get; }

    /// <summary>
    /// Gets the inner exception that caused the failure.
    /// </summary>
    public new Exception? InnerException { get; }

    public WorkItemExecutionException(
        string message,
        DesWorkItemComponent workItem,
        Exception? innerException)
        : base(message, innerException)
    {
        WorkItem = workItem;
        InnerException = innerException;
    }
}

/// <summary>
/// Interface for executable drivers.
/// </summary>
public interface IExecutableDriver : IDriver
{
    /// <summary>
    /// Executes the driver for the specified tick.
    /// </summary>
    /// <param name="tick">The current canonical tick.</param>
    void Execute(CanonicalTick tick);
}

/// <summary>
/// Interface for executable triggers.
/// </summary>
public interface IExecutableTrigger : ITrigger
{
    /// <summary>
    /// Executes the trigger for the specified tick.
    /// </summary>
    /// <param name="tick">The current canonical tick.</param>
    void Execute(CanonicalTick tick);
}
