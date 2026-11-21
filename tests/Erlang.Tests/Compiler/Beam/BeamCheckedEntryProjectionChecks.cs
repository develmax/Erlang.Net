using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedEntryProjectionChecks
{
    internal const string TestName = "compiler/beam/projects-checked-entry-fields-before-their-frame";
    private const string Fixture = "beam_checked_entry_projection_probe.erl";
    private const string WrongFrame = "The checked projection used the wrong frame or live roots.";
    private const string WrongResult = "Changing a checked projection's frame changed an observable result.";
    private const string Source = """
        -module(beam_checked_entry_projection_probe).
        -export([nested/2, simple/2, fallback/2, retained/2, retained_fallback/2, caught/2]).
        -record(outer, {value, extra}).
        -record(inner, {value, mark}).
        
        nested(#outer{value = #inner{value = Value}}, Key) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous}.
        simple(#outer{value = Value}, Key) when is_tuple(Value) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous}.
        fallback(#outer{value = #inner{value = Value}}, Key) ->
            Previous = erlang:put(Key, Value),
            {Value, Previous};
        fallback(_, _) -> fallback.
        retained(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = erlang:put(Key, Value),
            {Original, Value, Previous}.
        
        retained_fallback(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = erlang:put(Key, Value),
            {Original, Value, Previous};
        retained_fallback(_, Key) -> erlang:put(Key, fallback).
        
        caught(#outer{value = #inner{} = Value} = Original, Key) ->
            Previous = catch erlang:put(Key, Value),
            {Original, Value, Previous}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayouts(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term inner = Term.Tuple(Term.A("inner"), Term.A("value"), Term.A("mark"));
            Term outer = Term.Tuple(Term.A("outer"), inner, Term.A("spare"));
            Term rejected = Term.Tuple(Term.A("outer"), Term.A("wrong"), Term.A("spare"));
            Term prior = Term.A("undefined");
            Term expected = Term.Tuple(outer, inner, prior);
            if (!expected.Equals(await module.Invoke(
                context,
                "retained",
                outer,
                key
            ))
                || !inner.Equals(await module.Invoke(
                    context,
                    "retained_fallback",
                    rejected,
                    key
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "retained",
                    rejected,
                    key
                );
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckLayouts(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("retained");
        int allocate = Array.FindIndex(positive, instruction => Opcode(instruction) == Allocate);
        if (allocate < 0
            || !positive.Take(allocate).Any(instruction => Opcode(instruction) == GetTupleElement)
            || positive.Take(allocate).Count(instruction => Opcode(instruction) == IsTaggedTuple) != 2
            || Value(Operands(positive[allocate])[^1]) != 3
            || !KeepsFrameBeforeProjection("retained_fallback")
            || !KeepsFrameBeforeProjection("caught"))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        bool KeepsFrameBeforeProjection(string name)
        {
            object[] body = Body(name);
            int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
            int projected = Array.FindIndex(body, instruction => Opcode(instruction) == GetTupleElement);

            return frame >= 0 && projected > frame;
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
