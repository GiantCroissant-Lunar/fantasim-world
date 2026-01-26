using FantaSim.Space.Stellar.Contracts.Topology;

namespace FantaSim.Space.Stellar.Contracts.Validation;

public static class L3ValidationRules
{
    public static ValidationResult ValidateTopology(L3SystemTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        var errors = new List<string>();

        if (topology.RootBody is null)
        {
            errors.Add("System must have a root body.");
            return new ValidationResult(false, errors);
        }

        if (topology.RootBody.ParentId is not null)
            errors.Add("Root body must not have a parent.");

        if (topology.RootBody.Orbit is not null)
            errors.Add("Root body must not have an orbit.");

        var byId = new Dictionary<Guid, L3Body>();

        Traverse(topology.RootBody, expectedParentId: null);
        ValidateParentChains();

        return new ValidationResult(errors.Count == 0, errors);

        void Traverse(L3Body body, Guid? expectedParentId)
        {
            if (!byId.TryAdd(body.BodyId, body))
            {
                errors.Add($"Duplicate BodyId detected: {body.BodyId}.");
                return;
            }

            if (expectedParentId is null)
            {
                if (body.ParentId is not null)
                    errors.Add($"Root body {body.BodyId} must have ParentId null.");
                if (body.Orbit is not null)
                    errors.Add($"Root body {body.BodyId} must have Orbit null.");
            }
            else
            {
                if (body.ParentId is null)
                    errors.Add($"Body {body.BodyId} is non-root but has ParentId null.");
                else if (body.ParentId != expectedParentId)
                    errors.Add($"Body {body.BodyId} ParentId {body.ParentId} does not match container parent {expectedParentId}.");

                if (body.Orbit is null)
                    errors.Add($"Body {body.BodyId} has a parent but no orbit.");
                else if (!body.Orbit.Value.IsValid())
                    errors.Add($"Body {body.BodyId} has invalid orbital elements.");
            }

            if (string.IsNullOrWhiteSpace(body.Name))
                errors.Add($"Body {body.BodyId} has empty Name.");

            if (body.Properties is null)
                errors.Add($"Body {body.BodyId} has null Properties.");

            foreach (var child in body.Children)
                Traverse(child, expectedParentId: body.BodyId);
        }

        void ValidateParentChains()
        {
            foreach (var body in byId.Values)
            {
                var seen = new HashSet<Guid>();
                var current = body;

                while (current.ParentId.HasValue)
                {
                    if (!seen.Add(current.BodyId))
                    {
                        errors.Add($"Cycle detected in parent chain at BodyId {current.BodyId}.");
                        break;
                    }

                    var parentId = current.ParentId.Value;
                    if (!byId.TryGetValue(parentId, out var parent))
                    {
                        errors.Add($"Body {current.BodyId} references missing parent {parentId}.");
                        break;
                    }

                    current = parent;
                }
            }
        }
    }
}
