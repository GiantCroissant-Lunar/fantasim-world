using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fantasim.Architecture.Analyzers
{
    public enum ArchitectureLayer
    {
        Unknown,
        Truth,
        Derived,
        Tooling,
        Infrastructure
    }

    public static class LayerInference
    {
        public static ArchitectureLayer InferLayer(Compilation compilation, AnalyzerOptions options)
        {
            // 1. Attribute: [assembly: FantasimLayer(Layer.Derived)]
            var attributeLayer = GetLayerFromAttribute(compilation);
            if (attributeLayer != ArchitectureLayer.Unknown)
                return attributeLayer;

            // 2. MSBuild Property: $(FantasimLayer)
            // Note: Requires <CompilerVisibleProperty Include="FantasimLayer" /> in the project file
            // We check global options for the build property
            if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.FantasimLayer", out var buildPropValue))
            {
                if (Enum.TryParse<ArchitectureLayer>(buildPropValue, true, out var layer))
                    return layer;
            }

            // 3. Naming Convention
            var assemblyName = compilation.AssemblyName;
            if (string.IsNullOrEmpty(assemblyName))
                return ArchitectureLayer.Unknown;

            if (Regex.IsMatch(assemblyName, @"\.Truth(\.|$)"))
                return ArchitectureLayer.Truth;

            if (Regex.IsMatch(assemblyName, @"\.Derived(\.|$)"))
                return ArchitectureLayer.Derived;

            return ArchitectureLayer.Unknown;
        }

        private static ArchitectureLayer GetLayerFromAttribute(Compilation compilation)
        {
            // Look for [assembly: FantasimLayer(...)]
            // We scan assembly attributes.
            // Since we don't have a direct type reference to "FantasimLayerAttribute", we resolve by name.

            foreach (var attr in compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "FantasimLayerAttribute")
                {
                    if (attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is int intVal)
                    {
                         // Assuming enum mapping matches: 0=Unknown, 1=Truth, 2=Derived ?
                         // Or string? Let's assume it maps to our enum integers or name.
                         // For now, let's assume it passes the enum value.
                         return (ArchitectureLayer)intVal;
                    }
                    else if (attr.ConstructorArguments.Length > 0 &&
                             attr.ConstructorArguments[0].Value is string strVal)
                    {
                         if (Enum.TryParse<ArchitectureLayer>(strVal, true, out var layer))
                             return layer;
                    }
                }
            }
            return ArchitectureLayer.Unknown;
        }
    }
}
