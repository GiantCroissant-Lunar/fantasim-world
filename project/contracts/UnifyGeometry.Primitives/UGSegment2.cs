namespace UnifyGeometry;

public readonly record struct UGSegment2(UGPoint2 Start, UGPoint2 End)
{
    public bool IsEmpty => Start.IsEmpty || End.IsEmpty;

    public double Length => Start.DistanceTo(End);
}
