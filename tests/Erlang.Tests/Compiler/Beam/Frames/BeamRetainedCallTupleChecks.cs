using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedCallTupleChecks
{
    internal const string TestName = "compiler/beam/retained-call-tuple";
    private const string EarlyRead = "Keep the proven local tuple intact until its fields are needed.";
    private const string ExtraSlot = "Reuse the slot after loading the outgoing argument that last reads it.";
    private const string ShapeLost = "Keep the check on a result whose tuple shape is unknown.";

    private const string Source = """
        -module(beam_retained_call_tuple).
        -export([configure/1, dynamic/1]).

        configure(Input) ->
            Side = store:allocate(Input),
            {Handle, Name, Index} = make_result(Input),
            store:note(Side, Input),
            store:keep(Side, Handle),
            store:finish(Side),
            {saved, Side, {Name, Index}}.

        dynamic(Input) ->
            Side = store:allocate(Input),
            {Handle, Name, Index} = store:read(Input),
            store:note(Side, Input),
            store:keep(Side, Handle),
            store:finish(Side),
            {saved, Side, {Name, Index}}.

        make_result(Input) ->
            Handle = store:lookup(Input),
            Name = store:lookup({name, Input}),
            Index = store:lookup({index, Input}),
            {Handle, Name, Index}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_retained_call_tuple.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] configured = Body("configure");
        int producer = Array.FindIndex(configured, instruction => Opcode(instruction) == LocalCall);
        int firstEffect = Array.FindIndex(configured, producer + 1, instruction => Opcode(instruction) == ExternalCall);
        int lastEffect = Array.FindLastIndex(configured, instruction => Opcode(instruction) == ExternalCall);
        if (producer < 0 || firstEffect <= producer || lastEffect <= firstEffect
            || configured[(producer + 1)..firstEffect].Any(instruction => Opcode(instruction) == GetTupleElement)
            || !configured[(firstEffect + 1)..lastEffect].Any(instruction => IsProjection(instruction, 0, 1))
            || Opcode(configured[lastEffect + 1]) != TestHeap
            || !IsProjection(configured[lastEffect + 2], 1, 0)
            || !IsProjection(configured[lastEffect + 3], 2, 1))
        {
            throw new InvalidOperationException(EarlyRead);
        }

        object frame = configured.Single(instruction => Opcode(instruction) == Allocate);
        if ((BigInteger)Property(Items(frame, "Operands")[0], "Value") != 2
            || firstEffect <= producer + 4
            || !IsMove(
                configured[producer + 2],
                "y",
                1,
                "x",
                1
            )
            || !IsMove(
                configured[producer + 3],
                "x",
                0,
                "y",
                1
            ))
        {
            throw new InvalidOperationException(ExtraSlot);
        }

        object[] uncheckedResult = Body("dynamic");
        if (!uncheckedResult.Any(instruction => Opcode(instruction) == IsTuple)
            || !uncheckedResult.Any(instruction => Opcode(instruction) == TestArity))
        {
            throw new InvalidOperationException(ShapeLost);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static bool IsProjection(object instruction, int field, int register)
    {
        return Opcode(instruction) == GetTupleElement
            && Items(instruction, "Operands") is [var tuple, var index, var destination]
            && (string)Property(tuple, "Kind") == "y"
            && (BigInteger)Property(index, "Value") == field
            && (string)Property(destination, "Kind") == "x"
            && (BigInteger)Property(destination, "Value") == register;
    }

    private static bool IsMove(
        object instruction,
        string source,
        int sourceRegister,
        string target,
        int targetRegister
    )
    {
        return Opcode(instruction) == Move && Items(instruction, "Operands") is [var from, var to]
            && (string)Property(from, "Kind") == source && (BigInteger)Property(from, "Value") == sourceRegister
            && (string)Property(to, "Kind") == target && (BigInteger)Property(to, "Value") == targetRegister;
    }

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
