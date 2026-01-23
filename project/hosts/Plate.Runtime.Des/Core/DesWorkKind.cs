namespace Plate.Runtime.Des.Core;

public enum DesWorkKind : int
{
    // Reserved 0-99
    Undefined = 0,

    // Geosphere 100-199
    RunPlateSolver = 100,
    EmitBoundaryGeometryUpdate = 101,

    // Expansion...
}
