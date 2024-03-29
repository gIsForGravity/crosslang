using System.Runtime.InteropServices;

namespace crosslangnet;

[StructLayout(LayoutKind.Sequential)]
public struct FunctionParameter
{
    public FunctionParameterType Type;
    public ParameterUnion Value;
    
    public enum FunctionParameterType : byte
    {
        Byte,
        Short,
        Int,
        Long,
        Bool,
        Float,
        Double,
        Char,
        Obj
    }
    
    [StructLayout(LayoutKind.Explicit)]
    public struct ParameterUnion
    {
        [FieldOffset(0)] public byte ByteValue;
        [FieldOffset(0)] public short ShortValue;
        [FieldOffset(0)] public int IntValue;
        [FieldOffset(0)] public long LongValue;
        [FieldOffset(0)] public byte BoolValue;
        [FieldOffset(0)] public float FloatValue;
        [FieldOffset(0)] public double DoubleValue;
        [FieldOffset(0)] public char CharValue;
        [FieldOffset(0)] public long ObjValue;
    }
}