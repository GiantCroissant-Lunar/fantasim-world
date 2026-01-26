using Plate.TimeDete.Time.Primitives;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Derived;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Entities;
using FantaSim.Geosphere.Plate.Kinematics.Contracts.Numerics;
using FantaSim.Geosphere.Plate.Topology.Contracts.Entities;
using FantaSim.Geosphere.Plate.Topology.Contracts.Identity;
using FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

namespace FantaSim.Geosphere.Plate.Kinematics.Materializer;

public sealed class PlateKinematicsState : IPlateKinematicsStateView
{
    private readonly Dictionary<PlateId, Dictionary<MotionSegmentId, MotionSegment>> _segmentsByPlate = new();
    private readonly Dictionary<PlateId, List<MotionSegment>> _sortedSegmentsByPlate = new();

    public PlateKinematicsState(TruthStreamIdentity identity)
    {
        Identity = identity;
        LastEventSequence = -1;
    }

    public TruthStreamIdentity Identity { get; }

    public long LastEventSequence { get; private set; }

    public void SetLastEventSequence(long sequence) => LastEventSequence = sequence;

    public void UpsertSegment(
        PlateId plateId,
        MotionSegmentId segmentId,
        CanonicalTick tickA,
        CanonicalTick tickB,
        QuantizedEulerPoleRotation stageRotation)
    {
        if (tickB.Value <= tickA.Value)
            throw new InvalidOperationException($"Motion segment must satisfy TickA < TickB. Got {tickA.Value}..{tickB.Value}.");

        if (!_segmentsByPlate.TryGetValue(plateId, out var byId))
        {
            byId = new Dictionary<MotionSegmentId, MotionSegment>();
            _segmentsByPlate[plateId] = byId;
        }

        byId[segmentId] = new MotionSegment(segmentId, tickA, tickB, stageRotation);
    }

    public void RetireSegment(PlateId plateId, MotionSegmentId segmentId)
    {
        if (_segmentsByPlate.TryGetValue(plateId, out var byId))
        {
            byId.Remove(segmentId);
        }
    }

    public void RebuildIndices()
    {
        _sortedSegmentsByPlate.Clear();

        foreach (var (plateId, byId) in _segmentsByPlate)
        {
            var list = byId.Values.ToList();
            list.Sort(MotionSegmentComparer.Instance);
            _sortedSegmentsByPlate[plateId] = list;
        }
    }

    public bool TryGetRotation(PlateId plateId, CanonicalTick tick, out Quaterniond rotation)
    {
        rotation = Quaterniond.Identity;
        if (tick.Value <= 0)
            return true;

        if (!_sortedSegmentsByPlate.TryGetValue(plateId, out var segments) || segments.Count == 0)
            return true;

        rotation = ComputeAbsoluteRotation(segments, tick, new Dictionary<long, Quaterniond>());
        return true;
    }

    private static Quaterniond ComputeAbsoluteRotation(
        List<MotionSegment> segments,
        CanonicalTick tick,
        Dictionary<long, Quaterniond> memo)
    {
        if (tick.Value <= 0)
            return Quaterniond.Identity;

        if (memo.TryGetValue(tick.Value, out var cached))
            return cached;

        // Prefer a segment that covers (TickA, TickB] with TickA < tick <= TickB.
        MotionSegment? active = null;
        foreach (var seg in segments)
        {
            if (seg.TickA.Value < tick.Value && tick.Value <= seg.TickB.Value)
            {
                if (active is null || MotionSegmentComparer.Instance.Compare(seg, active.Value) < 0)
                {
                    active = seg;
                }
            }
        }

        Quaterniond result;

        if (active is not null)
        {
            var seg = active.Value;
            var baseRot = ComputeAbsoluteRotation(segments, seg.TickA, memo);
            var frac = (double)(tick.Value - seg.TickA.Value) / (seg.TickB.Value - seg.TickA.Value);
            var delta = RotationMath.FractionalStageRotation(seg.StageRotation, frac);
            result = RotationMath.Multiply(baseRot, delta);
        }
        else
        {
            // If no segment covers this tick, hold the last known end-rotation constant.
            MotionSegment? lastEnded = null;
            foreach (var seg in segments)
            {
                if (seg.TickB.Value <= tick.Value)
                {
                    if (lastEnded is null || seg.TickB.Value > lastEnded.Value.TickB.Value ||
                        (seg.TickB.Value == lastEnded.Value.TickB.Value &&
                         MotionSegmentComparer.Instance.Compare(seg, lastEnded.Value) < 0))
                    {
                        lastEnded = seg;
                    }
                }
            }

            if (lastEnded is null)
            {
                result = Quaterniond.Identity;
            }
            else
            {
                result = ComputeAbsoluteRotation(segments, lastEnded.Value.TickB, memo);
            }
        }

        memo[tick.Value] = result;
        return result;
    }

    public readonly record struct MotionSegment(
        MotionSegmentId SegmentId,
        CanonicalTick TickA,
        CanonicalTick TickB,
        QuantizedEulerPoleRotation StageRotation);

    private sealed class MotionSegmentComparer : IComparer<MotionSegment>
    {
        public static readonly MotionSegmentComparer Instance = new();

        public int Compare(MotionSegment x, MotionSegment y)
        {
            // For deterministic selection, sort by:
            // - TickA descending (latest start first),
            // - TickB ascending (shorter first),
            // - SegmentId ascending.
            var cmp = y.TickA.Value.CompareTo(x.TickA.Value);
            if (cmp != 0) return cmp;

            cmp = x.TickB.Value.CompareTo(y.TickB.Value);
            if (cmp != 0) return cmp;

            return x.SegmentId.Value.CompareTo(y.SegmentId.Value);
        }
    }

    private static class RotationMath
    {
        private const double DegToRad = Math.PI / 180.0;

        public static Quaterniond FractionalStageRotation(QuantizedEulerPoleRotation rot, double fraction)
        {
            fraction = Math.Clamp(fraction, 0.0, 1.0);

            var axis = EulerPoleToUnitVector(rot.PoleLonDeg, rot.PoleLatDeg);
            var angleRad = rot.AngleDeg * DegToRad * fraction;
            return Quaterniond.FromAxisAngle(axis, angleRad);
        }

        private static Vector3d EulerPoleToUnitVector(double lonDeg, double latDeg)
        {
            var lon = lonDeg * DegToRad;
            var lat = latDeg * DegToRad;

            var cosLat = Math.Cos(lat);
            return new Vector3d(
                cosLat * Math.Cos(lon),
                cosLat * Math.Sin(lon),
                Math.Sin(lat));
        }

        public static Quaterniond Multiply(Quaterniond a, Quaterniond b)
        {
            // Hamilton product (a * b).
            var w = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
            var x = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
            var y = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
            var z = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;
            return Normalize(new Quaterniond(x, y, z, w));
        }

        private static Quaterniond Normalize(Quaterniond q)
        {
            var norm = Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
            if (norm <= 0)
                return Quaterniond.Identity;
            return new Quaterniond(q.X / norm, q.Y / norm, q.Z / norm, q.W / norm);
        }
    }
}
