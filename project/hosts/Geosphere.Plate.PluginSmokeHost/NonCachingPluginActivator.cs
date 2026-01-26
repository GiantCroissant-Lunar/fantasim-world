using System;
using System.Linq;
using PluginArchi.Extensibility.Abstractions;

namespace FantaSim.Geosphere.Plate.PluginSmokeHost;

public sealed class NonCachingPluginActivator : IPluginActivator
{
    public bool CanActivate(Type pluginType)
        => pluginType is not null
           && !pluginType.IsAbstract
           && typeof(IPlugin).IsAssignableFrom(pluginType)
           && pluginType.GetConstructors().Length > 0;

    public IPlugin Activate(Type pluginType, IServiceProvider services)
    {
        if (!CanActivate(pluginType))
        {
            throw new PluginLoadException($"Cannot activate plugin type {pluginType?.FullName}.")
            {
                PluginId = pluginType?.FullName,
                Reason = PluginLoadFailureReason.ActivationFailed
            };
        }

        var constructors = pluginType!
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];
            var ok = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var service = services.GetService(p.ParameterType);

                if (service is not null)
                {
                    args[i] = service;
                }
                else if (p.HasDefaultValue)
                {
                    args[i] = p.DefaultValue;
                }
                else
                {
                    ok = false;
                    break;
                }
            }

            if (!ok)
            {
                continue;
            }

            return (IPlugin)ctor.Invoke(args);
        }

        throw new PluginLoadException($"Cannot activate plugin type {pluginType?.FullName}: no constructor could be satisfied from the host services.")
        {
            PluginId = pluginType?.FullName,
            Reason = PluginLoadFailureReason.DependencyMissing
        };
    }
}
