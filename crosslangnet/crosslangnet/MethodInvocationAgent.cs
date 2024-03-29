using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace crosslangnet;

public class MethodInvocationAgent
{
    public static byte ByteFunction(string s)
    {
        Console.WriteLine(s);

        return 5;
    }
    
    private delegate void StaticFunctionLambdaDelegate(Dictionary<long, object> objectLookup,
        Span<FunctionParameter> parameterList);
    private delegate void MethodLambdaDelegate(object objectToInvokeOn, Dictionary<long, object> objectLookup,
        Span<FunctionParameter> parameterList);

    private Dictionary<long, StaticFunctionLambdaDelegate> _functionDelegates = new();
    private Dictionary<long, MethodLambdaDelegate> _methodDelegates = new();

    private Dictionary<long, MethodInfo> _functions = new();
    private Dictionary<long, MethodInfo> _methods = new();

    /// <summary>
    /// Creates an id for a static function and registers it
    /// </summary>
    /// <param name="method">the function to create the id for</param>
    /// <returns>the newly created id</returns>
    public long CreateStaticFunctionId(MethodInfo func)
    {
        long functionId = FindUnusedId(_functions);
        _functions[functionId] = func;
        // long methodId = FindUnusedId(_functionDelegates);
        // _functionDelegates[methodId] = CreateFunctionDelegate(method);

        return functionId;
    }
    
    public long CreateMethodFunctionId(MethodInfo method)
    {
        long methodId = FindUnusedId(_methods);
        _methods[methodId] = method;
        // long methodId = FindUnusedId(_methodDelegates);
        // _methodDelegates[methodId] = CreateMethodDelegate(method);

        return methodId;
    }

    /// <summary>
    /// Calls a static function by id and returns its result as a ReturnValue
    /// </summary>
    /// <param name="functionId">the id of the function</param>
    /// <param name="objectLookup">A dictionary for looking up objects</param>
    /// <param name="parameters">A span containing the parameters to pass to the function</param>
    /// <exception cref="NotImplementedException">I haven't implemented reference types yet</exception>
    /// <exception cref="NotSupportedException">The type passed in is currently unsupported</exception>
    /// <returns>The result of the function as a ReturnValue</returns>
    public ReturnValue CallStaticFunction(long functionId, Dictionary<long, object> objectLookup, Span<FunctionParameter> parameters)
    {
        var retValue = _functions[functionId].Invoke(null, Convert(parameters, objectLookup));
        // Console.WriteLine($"return value type: {retValue?.GetType()}, return value: {retValue}");

        var asRetValue = ToReturnValue(_functions[functionId].Invoke(null, Convert(parameters, objectLookup)));
        // Console.WriteLine($"asRetVal.Type: {asRetValue.Type}, asRetVal.Int.Value: {asRetValue.Value.IntValue}");

        return asRetValue;
        // _functionDelegates[functionId](objectLookup, parameters);
    }
    
    public void CallObjectMethod(long methodId, object objectToInvokeOn, Dictionary<long, object> objectLookup, Span<FunctionParameter> parameters)
    {
        _methods[methodId].Invoke(null, Convert(parameters, objectLookup));
        // _methodDelegates[methodId](objectToInvokeOn, objectLookup, parameters);
    }

    [Pure]
    private static object[] Convert(Span<FunctionParameter> parameters, Dictionary<long, object> objectLookup)
    {
        var objects = new List<object>();
        foreach (var param in parameters)
        {
            objects.Add(param.Type switch
            {
                FunctionParameter.FunctionParameterType.Byte => param.Value.ByteValue,
                FunctionParameter.FunctionParameterType.Short => param.Value.ShortValue,
                FunctionParameter.FunctionParameterType.Int => param.Value.IntValue,
                FunctionParameter.FunctionParameterType.Long => param.Value.LongValue,
                FunctionParameter.FunctionParameterType.Bool => param.Value.BoolValue,
                FunctionParameter.FunctionParameterType.Float => param.Value.FloatValue,
                FunctionParameter.FunctionParameterType.Double => param.Value.DoubleValue,
                FunctionParameter.FunctionParameterType.Char => param.Value.CharValue,
                FunctionParameter.FunctionParameterType.Obj => objectLookup[param.Value.ObjValue],
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        return objects.ToArray();
    }

    /// <summary>
    /// Creates a return value type from an arbitrary object.
    /// Temporary until I decide to use Reflection.Emit instead
    /// of regular old boring reflection
    /// </summary>
    /// <param name="obj">The object to create a return value of</param>
    /// <returns>The object encoded as a ReturnValue struct</returns>
    /// <exception cref="NotImplementedException">I haven't implemented reference types yet</exception>
    /// <exception cref="NotSupportedException">The type passed in is currently unsupported</exception>
    private static ReturnValue ToReturnValue(object? obj)
    {
        var objType = obj?.GetType();
        var retValue = new ReturnValue();

        if (obj == null)
        {
            retValue.Type = ReturnValue.ReturnValueType.None;
        } else if (objType?.GetTypeInfo().IsClass is true)
        {
            throw new NotImplementedException("returning an object isn't implemented yet");
        } else if (objType == typeof(byte))
        {
            retValue.Type = ReturnValue.ReturnValueType.Byte;
            retValue.Value.ByteValue = (byte) obj;
        } else if (objType == typeof(short))
        {
            retValue.Type = ReturnValue.ReturnValueType.Short;
            retValue.Value.ShortValue = (short) obj;
        } else if (objType == typeof(int))
        {
            retValue.Type = ReturnValue.ReturnValueType.Int;
            retValue.Value.IntValue = (int) obj;
        } else if (objType == typeof(long))
        {
            retValue.Type = ReturnValue.ReturnValueType.Long;
            retValue.Value.LongValue = (long)obj;
        } else if (objType == typeof(bool))
        {
            retValue.Type = ReturnValue.ReturnValueType.Bool;
            retValue.Value.BoolValue = (bool) obj switch
            {
                true => 1,
                false => 0
            };
        } else if (objType == typeof(short))
        {
            retValue.Type = ReturnValue.ReturnValueType.Short;
            retValue.Value.ShortValue = (short) obj;
        } else if (objType == typeof(float))
        {
            retValue.Type = ReturnValue.ReturnValueType.Float;
            retValue.Value.FloatValue = (float) obj;
        } else if (objType == typeof(double))
        {
            retValue.Type = ReturnValue.ReturnValueType.Double;
            retValue.Value.DoubleValue = (double) obj;
        }
        else
        {
            throw new NotSupportedException("passing type not currently supported");
        }

        return retValue;
    }

    /// <summary>
    /// Finds an unused id inside a dictionary
    /// </summary>
    /// <param name="dictionary">The dictionary to check</param>
    /// <typeparam name="T">The type of the dictionary</typeparam>
    /// <returns>The found id</returns>
    [Pure]
    private static long FindUnusedId<T>(Dictionary<long, T> dictionary)
    {
        var random = Random.Shared;
        long id;
        do
        {
            id = random.NextInt64();
        } while (dictionary.ContainsKey(id));

        return id;
    }

    /// <summary>
    /// Creates a delegate that can be used to call a static function with a list of parameters. Slow!
    /// </summary>
    /// <param name="info">the function to create the delegate for</param>
    /// <returns>the newly created delegate</returns>
    /// <exception cref="ArgumentNullException">thrown if unable to find Dictionary.Item or fields inside FunctionParameter</exception>
    /// <exception cref="NotSupportedException">throw if the function contains a primitive type that doesn't exist in java</exception>
    [Pure]
    private static StaticFunctionLambdaDelegate CreateFunctionDelegate(MethodInfo info)
    {
        ParameterInfo[] parameters = info.GetParameters();

        // make a function at runtime that calls the function using a bunch of parameter objects
        // needs to be fast
        var objectLookupDictionary = Expression.Parameter(typeof(Dictionary<long, object>), "objectLookup");
        var functionParameterList = Expression.Parameter(typeof(Span<FunctionParameter>), "parameterList");

        Expression[] parameterExpressions = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type parameterType = parameters[i].ParameterType;
            FieldInfo typeField;

            if (parameterType.GetTypeInfo().IsClass)
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ObjValue)) ?? throw new ArgumentNullException();

                parameterExpressions[i] = Expression.Property(objectLookupDictionary,
                    typeof(Dictionary<long, object>).GetProperty("Item") ?? throw new ArgumentNullException(),
                    Expression.Field(Expression.Field(
                            Expression.Property(functionParameterList,
                                typeof(Span<FunctionParameter>).GetProperty("Item") ??
                                throw new ArgumentNullException(),
                                Expression.Constant(i, typeof(int))),
                            typeof(FunctionParameter).GetField(nameof(FunctionParameter.Value)) ??
                            throw new ArgumentNullException()),
                        typeField));
                continue;
            }

            if (parameterType == typeof(byte))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ByteValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(short))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ShortValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(int))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.IntValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(long))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.LongValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(bool))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.BoolValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(float))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.FloatValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(double))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.DoubleValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(char))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.CharValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(short))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ObjValue)) ?? throw new ArgumentNullException();
            }
            else
            {
                throw new NotSupportedException();
            }

            parameterExpressions[i] = Expression.Field(Expression.Field(
                    Expression.Property(functionParameterList,
                        typeof(Span<FunctionParameter>).GetProperty("Item") ?? throw new ArgumentNullException(),
                        Expression.Constant(i, typeof(int))),
                    typeof(FunctionParameter).GetField(nameof(FunctionParameter.Value)) ??
                    throw new ArgumentNullException()),
                typeField);
        }

        return Expression.Lambda<StaticFunctionLambdaDelegate>(Expression.Call(null, info, parameterExpressions),
            objectLookupDictionary, functionParameterList).Compile();
    }

    /// <summary>
    /// Creates a delegate that can be used to call a nonstatic method with a list of parameters. Slow!
    /// </summary>
    /// <param name="info">the method to create the delegate for</param>
    /// <returns>the newly created delegate</returns>
    /// <exception cref="ArgumentNullException">thrown if unable to find Dictionary.Item or fields inside FunctionParameter</exception>
    /// <exception cref="NotSupportedException">throw if the function contains a primitive type that doesn't exist in java</exception>
    [Pure]
    private MethodLambdaDelegate CreateMethodDelegate(MethodInfo info)
    {
        ParameterInfo[] parameters = info.GetParameters();

        // make a function at runtime that calls the function using a bunch of parameter objects
        // needs to be fast
        var objectToInvokeOn = Expression.Parameter(typeof(object), "objectToInvokeOn");
        var objectLookupDictionary = Expression.Parameter(typeof(Dictionary<long, object>), "objectLookup");
        var functionParameterList = Expression.Parameter(typeof(Span<FunctionParameter>), "parameterList");

        Expression[] parameterExpressions = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type parameterType = parameters[i].ParameterType;
            FieldInfo typeField;

            if (parameterType.GetTypeInfo().IsClass)
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ObjValue)) ?? throw new ArgumentNullException();

                parameterExpressions[i] = Expression.Property(objectLookupDictionary,
                    typeof(Dictionary<long, object>).GetProperty("Item") ?? throw new ArgumentNullException(),
                    Expression.Field(Expression.Field(
                            Expression.Property(functionParameterList,
                                typeof(Span<FunctionParameter>).GetProperty("Item") ??
                                throw new ArgumentNullException(),
                                Expression.Constant(i, typeof(int))),
                            typeof(FunctionParameter).GetField(nameof(FunctionParameter.Value)) ??
                            throw new ArgumentNullException()),
                        typeField));
                continue;
            }

            if (parameterType == typeof(byte))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ByteValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(short))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ShortValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(int))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.IntValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(long))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.LongValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(bool))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.BoolValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(float))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.FloatValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(double))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.DoubleValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(char))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.CharValue)) ?? throw new ArgumentNullException();
            }
            else if (parameterType == typeof(short))
            {
                typeField = typeof(FunctionParameter.ParameterUnion).GetField(
                    nameof(FunctionParameter.ParameterUnion.ObjValue)) ?? throw new ArgumentNullException();
            }
            else
            {
                throw new NotSupportedException();
            }

            parameterExpressions[i] = Expression.Field(Expression.Field(
                    Expression.Property(functionParameterList,
                        typeof(Span<FunctionParameter>).GetProperty("Item") ?? throw new ArgumentNullException(),
                        Expression.Constant(i, typeof(int))),
                    typeof(FunctionParameter).GetField(nameof(FunctionParameter.Value)) ??
                    throw new ArgumentNullException()),
                typeField);
        }

        return Expression.Lambda<MethodLambdaDelegate>(Expression.Call(objectToInvokeOn, info, parameterExpressions),
            objectToInvokeOn, objectLookupDictionary, functionParameterList).Compile();
    }
}