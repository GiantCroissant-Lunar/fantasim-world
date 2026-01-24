namespace UnifyGeometry;

public readonly record struct UGTriangle2(UGPoint2 A, UGPoint2 B, UGPoint2 C)
{
    public bool IsEmpty => A.IsEmpty || B.IsEmpty || C.IsEmpty;
}
