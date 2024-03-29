using System.Runtime.InteropServices;

namespace crosslangnet;

[StructLayout(LayoutKind.Sequential)]
public struct DotnetInteropResultLong
{
    public ResultType Type;
    public ResultValue Value;

    public enum ResultType : byte
    {
        Ok,
        Err
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ResultValue
    {
        [FieldOffset(0)] public long OkValue;
        [FieldOffset(0)] public ErrorKind ErrValue;
    }

    public enum ErrorKind : int
    {
        StringIsNull,
        MethodNotFound,
        NotImplemented,
        ReturnTypeNotSupported,
    }
}