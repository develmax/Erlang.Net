using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class BeamReceiveMarkerTypeAccess
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static Type Type(string name) => typeof(Mailbox).Assembly.GetType(BeamReceiveMarkerTypeHostNames.RuntimeNamespace + name, true)!;

    internal static object New(string name, params object?[] values)
        => Activator.CreateInstance(
            Type(name),
            Members,
            null,
            values,
            null
        )!;

    internal static object Operand(string kind, int index)
        => New(
            BeamReceiveMarkerTypeHostNames.OperandType,
            kind,
            new BigInteger(index),
            null
        );

    internal static object Call(object owner, string name, params object?[] values)
    {
        Type type = owner is Type selected ? selected : owner.GetType();
        MethodInfo method = type.GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == values.Length
            && candidate.GetParameters().Select((parameter, index) => values[index] is null
                ? !parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) is not null
                : parameter.ParameterType.IsInstanceOfType(values[index])).All(value => value));
        try
        {
            return method.Invoke(owner is Type ? null : owner, values)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static int ManagedSlotSize()
    {
        Type slot = Type(BeamReceiveMarkerTypeHostNames.SlotType);
        var method = new DynamicMethod(
            BeamReceiveMarkerTypeHostNames.SlotType,
            typeof(int),
            System.Type.EmptyTypes,
            typeof(BeamReceiveMarkerTypeAccess).Module,
            true
        );
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Sizeof, slot);
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<int>>()();
    }

    internal static void Refuses(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            refused = true;
        }
        Require(refused);
    }

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamReceiveMarkerTypeHostNames.Failure);
    }
}
