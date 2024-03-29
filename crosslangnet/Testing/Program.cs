// See https://aka.ms/new-console-template for more information

using System.Reflection;
using crosslangnet;
using crosslangnet.Tests;

Console.WriteLine("Hello, World!");
Console.WriteLine(typeof(AddTest).GetTypeInfo().AssemblyQualifiedName);

var agent = new MethodInvocationAgent();

var writeLineId =
    agent.CreateStaticFunctionId(typeof(MethodInvocationAgent).GetMethod(nameof(MethodInvocationAgent.ByteFunction)) ?? throw new ArgumentNullException());

var objectLookup = new Dictionary<long, object>();
objectLookup[13] = "printing something lmao";

Span<FunctionParameter> parameters = new FunctionParameter[1];
parameters[0].Type = FunctionParameter.FunctionParameterType.Obj;
parameters[0].Value.ObjValue = 13;

var result = agent.CallStaticFunction(writeLineId, objectLookup, parameters);
Console.WriteLine(result.Type.ToString());
Console.WriteLine(result.Value.ByteValue);
