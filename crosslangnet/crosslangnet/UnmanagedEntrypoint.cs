using System.Reflection;
using System.Runtime.InteropServices;

namespace crosslangnet;

public static class UnmanagedEntrypoint
{
    private static readonly MethodInvocationAgent Agent = new();
    private static readonly Dictionary<long, object> ObjectLookup = new();

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnmanagedFunctionPointers
    {
        public delegate* unmanaged<IntPtr, IntPtr, nuint, IntPtr, nuint, DotnetInteropResultLong>
            CreateStaticFunctionId;
        
        public delegate* unmanaged<IntPtr, long, IntPtr, nuint, DotnetInteropResultReturnValue> CallStaticFunction;
    }

    [UnmanagedCallersOnly]
    private static unsafe DotnetInteropResultReturnValue CallStaticFunction(IntPtr libState, long functionId, IntPtr paramArrayBlit,
        nuint length)
    {
        var paramArray = (FunctionParameter*) paramArrayBlit;
        var result = new DotnetInteropResultReturnValue();

        try
        {
            var paramSlice = new Span<FunctionParameter>(paramArray, (int)length);
            var retVal = Agent.CallStaticFunction(functionId, ObjectLookup, paramSlice);

            result.Type = DotnetInteropResultReturnValue.ResultType.Ok;
            result.Value.OkValue = retVal;
            return result;
        }
        catch (NotImplementedException e)
        {
            result.Type = DotnetInteropResultReturnValue.ResultType.Err;
            result.Value.ErrValue = DotnetInteropResultReturnValue.ErrorKind.NotImplemented;
            return result;
        }
        catch (NotSupportedException e)
        {
            result.Type = DotnetInteropResultReturnValue.ResultType.Err;
            result.Value.ErrValue = DotnetInteropResultReturnValue.ErrorKind.ReturnTypeNotSupported;
            return result;
        }
    }

    [UnmanagedCallersOnly]
    private static DotnetInteropResultLong CreateStaticFunctionId(IntPtr libState, IntPtr fullyQualifiedTypeNamePtr, nuint fullyQualifiedTypeNameLength, IntPtr methodNamePtr, nuint methodNameLength)
    {
        // Console.WriteLine("Calling CreateStaticFunctionId");
        var result = new DotnetInteropResultLong();
        
        var fullyQualifiedTypeName = Marshal.PtrToStringUTF8(fullyQualifiedTypeNamePtr, (int) fullyQualifiedTypeNameLength);
        var methodName = Marshal.PtrToStringUTF8(methodNamePtr, (int) methodNameLength);
        if (fullyQualifiedTypeName is null || methodName is null)
        {
            result.Type = DotnetInteropResultLong.ResultType.Err;
            // result.Value = new DotnetInteropResultLong.ResultValue();
            result.Value.ErrValue = DotnetInteropResultLong.ErrorKind.StringIsNull;

            return result;
        }
        
        // Console.WriteLine($"fullyQualifiedTypeName: \"{fullyQualifiedTypeName}\", methodName: \"{methodName}\"");

        var type = Type.GetType(fullyQualifiedTypeName);
        var method = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (type is null || method is null)
        {
            result.Type = DotnetInteropResultLong.ResultType.Err;
            // result.Value = new DotnetInteropResultLong.ResultValue();
            result.Value.ErrValue = DotnetInteropResultLong.ErrorKind.MethodNotFound;

            return result;
        }

        result.Type = DotnetInteropResultLong.ResultType.Ok;
        result.Value.OkValue = Agent.CreateStaticFunctionId(method);
        return result;
    }

    [UnmanagedCallersOnly]
    private static unsafe DotnetInteropResultReturnValue CallMethodFunction(IntPtr libState, long functionId, long classObj, IntPtr paramArrayBlit,
        nuint length)
    {
        var paramArray = (FunctionParameter*) paramArrayBlit;
        var result = new DotnetInteropResultReturnValue();

        try
        {
            var paramSlice = new Span<FunctionParameter>(paramArray, (int)length);
            var retVal = Agent.CallStaticFunction(functionId, ObjectLookup, paramSlice);

            result.Type = DotnetInteropResultReturnValue.ResultType.Ok;
            result.Value.OkValue = retVal;
            return result;
        }
        catch (NotImplementedException)
        {
            result.Type = DotnetInteropResultReturnValue.ResultType.Err;
            result.Value.ErrValue = DotnetInteropResultReturnValue.ErrorKind.NotImplemented;
            return result;
        }
        catch (NotSupportedException)
        {
            result.Type = DotnetInteropResultReturnValue.ResultType.Err;
            result.Value.ErrValue = DotnetInteropResultReturnValue.ErrorKind.ReturnTypeNotSupported;
            return result;
        }
    }

    [UnmanagedCallersOnly]
    private static DotnetInteropResultLong CreateMethodFunctionId(IntPtr libState, IntPtr fullyQualifiedTypeNamePtr, nuint fullyQualifiedTypeNameLength, IntPtr methodNamePtr, nuint methodNameLength)
    {
        // Console.WriteLine("Calling CreateStaticFunctionId");
        var result = new DotnetInteropResultLong();
        
        var fullyQualifiedTypeName = Marshal.PtrToStringUTF8(fullyQualifiedTypeNamePtr, (int) fullyQualifiedTypeNameLength);
        var methodName = Marshal.PtrToStringUTF8(methodNamePtr, (int) methodNameLength);
        if (fullyQualifiedTypeName is null || methodName is null)
        {
            result.Type = DotnetInteropResultLong.ResultType.Err;
            // result.Value = new DotnetInteropResultLong.ResultValue();
            result.Value.ErrValue = DotnetInteropResultLong.ErrorKind.StringIsNull;

            return result;
        }
        
        // Console.WriteLine($"fullyQualifiedTypeName: \"{fullyQualifiedTypeName}\", methodName: \"{methodName}\"");

        var type = Type.GetType(fullyQualifiedTypeName);
        var method = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (type is null || method is null)
        {
            result.Type = DotnetInteropResultLong.ResultType.Err;
            // result.Value = new DotnetInteropResultLong.ResultValue();
            result.Value.ErrValue = DotnetInteropResultLong.ErrorKind.MethodNotFound;

            return result;
        }

        result.Type = DotnetInteropResultLong.ResultType.Ok;
        result.Value.OkValue = Agent.CreateStaticFunctionId(method);
        return result;
    }
    
    [UnmanagedCallersOnly]
    public static unsafe UnmanagedFunctionPointers CreateUnmanagedFunctionPointers()
    {
        return new UnmanagedFunctionPointers
        {
            CreateStaticFunctionId = &CreateStaticFunctionId,
            CallStaticFunction = &CallStaticFunction
        };
    }
}
