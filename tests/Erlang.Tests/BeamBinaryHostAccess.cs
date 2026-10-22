using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang;

namespace Erlang.Tests;

internal static class BeamBinaryHostAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static System.Type Type(string name) => typeof(BeamModule).Assembly.GetType(BeamBinaryHostNames.Namespace + name, true)!;

    internal static object New(string name, params object?[] values) => Activator.CreateInstance(
        Type(name),
        Members,
        null,
        values,
        null
    )!;

    internal static object Op(string kind, int value) => New(
        BeamBinaryHostNames.Operand,
        kind,
        new BigInteger(value),
        null
    );

    internal static Array Ops(params object[] values)
    {
        Array result = Array.CreateInstance(Type(BeamBinaryHostNames.Operand), values.Length);
        for (int index = 0; index < values.Length; index++)
            result.SetValue(values[index], index);

        return result;
    }

    internal static object Call(object owner, string method, params object?[] values)
    {
        System.Type type = owner is System.Type selected ? selected : owner.GetType();
        MethodInfo target = type.GetMethods(Members).Single(candidate => candidate.Name == method
            && candidate.GetParameters().Length == values.Length
            && candidate.GetParameters().Select((parameter, index) => values[index] is null
                ? !parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) is not null
                : parameter.ParameterType.IsInstanceOfType(values[index])).All(value => value));
        try
        {
            return target.Invoke(owner is System.Type ? null : owner, values)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static object Property(object owner, string name) => owner.GetType().GetProperty(name, Members)!.GetValue(owner)!;

    internal static void SetPosition(object context, int value) => context.GetType().GetProperty(BeamBinaryHostNames.Position, Members)!.SetValue(context, value);

    internal static object Code(byte[] image) => typeof(BeamModule).GetField(BeamBinaryHostNames.CodeField, Members)!.GetValue(BeamModule.Prepare(image))!;

    internal static void Validate(
        object code,
        string name,
        byte opcode,
        int arity,
        Array operands
    )
    {
        object definition = New(
            BeamBinaryHostNames.Definition,
            opcode,
            name,
            arity,
            false
        );
        object instruction = New(
            BeamBinaryHostNames.Instruction,
            0,
            definition,
            operands
        );
        Call(
            Type(BeamBinaryHostNames.Validation),
            "Validate",
            instruction,
            Property(code, BeamBinaryHostNames.StructureProperty)
        );
    }

    internal static void Refuses<T>(Action action) where T : Exception
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (T)
        {
            refused = true;
        }
        Require(refused);
    }

    internal static void Require(bool valid)
    {
        if (!valid)
            throw new InvalidOperationException(BeamBinaryHostNames.Failure);
    }
}
