using System;
using System.Runtime.InteropServices;
using MessagePack;

namespace FantaSim.Geosphere.Plate.Topology.Contracts.Numerics;

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct Vector3d(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z)
{
    public static Vector3d Zero => new(0, 0, 0);
    public static Vector3d UnitX => new(1, 0, 0);
    public static Vector3d UnitY => new(0, 1, 0);
    public static Vector3d UnitZ => new(0, 0, 1);

    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared() => X * X + Y * Y + Z * Z;

    public double Dot(Vector3d other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3d Cross(Vector3d other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    public Vector3d Normalize()
    {
        var len = Length();
        return len > double.Epsilon ? this / len : Zero;
    }

    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3d operator -(Vector3d a) => new(-a.X, -a.Y, -a.Z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3d operator *(Vector3d a, double d) => new(a.X * d, a.Y * d, a.Z * d);
    public static Vector3d operator /(Vector3d a, double d) => new(a.X / d, a.Y / d, a.Z / d);
}

[StructLayout(LayoutKind.Auto)]
[MessagePackObject]
public readonly record struct Quaterniond(
    [property: Key(0)] double X,
    [property: Key(1)] double Y,
    [property: Key(2)] double Z,
    [property: Key(3)] double W)
{
    public static Quaterniond Identity => new(0, 0, 0, 1);

    public Quaterniond Conjugate()
        => new(-X, -Y, -Z, W);

    public Quaterniond Inverse()
    {
        var norm = (X * X) + (Y * Y) + (Z * Z) + (W * W);
        if (norm == 0d)
            return Identity;

        var c = Conjugate();
        return new Quaterniond(c.X / norm, c.Y / norm, c.Z / norm, c.W / norm);
    }

    public static Quaterniond Multiply(Quaterniond a, Quaterniond b)
    {
        return new Quaterniond(
            (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
            (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
            (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
            (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));
    }

    public static Quaterniond FromAxisAngle(Vector3d axis, double angle)
    {
        double halfAngle = angle * 0.5;
        double s = Math.Sin(halfAngle);
        double c = Math.Cos(halfAngle);
        return new Quaterniond(axis.X * s, axis.Y * s, axis.Z * s, c);
    }

    public static double Angle(Quaterniond a, Quaterniond b)
    {
        double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        return Math.Acos(Math.Min(Math.Abs(dot), 1.0)) * 2.0;
    }
}
