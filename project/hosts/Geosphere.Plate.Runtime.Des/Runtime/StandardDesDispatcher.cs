using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FantaSim.Geosphere.Plate.Runtime.Des.Core;
using FantaSim.Geosphere.Plate.Runtime.Des.Drivers;
using FantaSim.Geosphere.Plate.Runtime.Des.Events;

namespace FantaSim.Geosphere.Plate.Runtime.Des.Runtime;

public sealed class StandardDesDispatcher : IDesDispatcher
{
    private readonly Dictionary<DesWorkKind, (IDriver Driver, ITrigger Trigger)> _registry = new();

    public void Register(DesWorkKind kind, IDriver driver, ITrigger trigger)
    {
        _registry[kind] = (driver, trigger);
    }

    public async Task<IReadOnlyList<ITruthEventDraft>> DispatchAsync(
        ScheduledWorkItem item,
        DesContext context,
        CancellationToken ct)
    {
        if (!_registry.TryGetValue(item.Kind, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for DesWorkKind {item.Kind}");
        }

        var (driver, trigger) = handler;

        // In a real system we might validate driver.Sphere == item.Sphere

        var output = await driver.EvaluateAsync(context, ct);

        return trigger.EmitDrafts(output, context.CurrentTick);
    }
}
