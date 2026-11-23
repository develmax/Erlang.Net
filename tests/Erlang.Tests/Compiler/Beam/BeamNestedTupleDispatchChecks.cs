using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedTupleDispatchChecks
{
    internal const string TestName = "compiler/beam/selects-nested-tuple-arity-before-branch-frames";
    private const string Fixture = "beam_nested_tuple_dispatch_probe.erl";
    private const string MissingDispatch = "Nested tuple arity must be selected before any branch allocates a frame.";
    private const string RepeatedBinding = "A repeated entry binding must remain an exact comparison.";

    private const string Source = """
        -module(beam_nested_tuple_dispatch_probe).
        -export([route/3, partial/3, repeated/2]).

        route({done,{value,V,Info},_}, Context, _) ->
            case decode(V) of
                {ok,Decoded} -> {decoded,Decoded,Info,Context};
                _ -> {invalid,{Context,Info}}
            end;
        route({done,{error,Code,Info},_}, Context, _) -> {bad,Code,{Context,Info}};
        route({done,{eof,_},_}, Context, Rest) when Rest == [] -> lists:reverse(Context);
        route({done,{eof,Info},_}, Context, _) -> {finish,{Context,Info}};
        route({more,Value}, _, _) -> Value;
        route(_, _, _) -> fallback.

        partial({done,{value,V,Info},_}, Context, _) ->
            case decode(V) of
                {ok,Decoded} -> {decoded,Decoded,Info,Context};
                _ -> {invalid,{Context,Info}}
            end;
        partial({done,{error,Code,Info},_}, Context, _) -> {bad,Code,{Context,Info}};
        partial({done,{eof,_},_}, Context, Rest) when Rest == [] -> lists:reverse(Context);
        partial({done,{eof,Info},_}, Context, _) -> {finish,{Context,Info}};
        partial({more,Value}, _, _) -> Value.

        repeated({done,{value,V,Info},V}, Context) -> {matched,V,Info,Context};
        repeated({done,{eof,Info},_}, Context) -> {eof,Info,Context};
        repeated({more,Value}, _) -> Value;
        repeated(_, _) -> fallback.

        decode({ctl,V}) -> {ok,V};
        decode(_) -> no.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (string name in new[] { "route", "partial" })
        {
            int aritySelections = Body(name).TakeWhile(instruction => Opcode(instruction) is not (Allocate or AllocateHeap))
                .Count(instruction => Opcode(instruction) == SelectTupleArity);
            if (aritySelections < 2)
            {
                throw new InvalidOperationException(MissingDispatch);
            }
        }

        if (!Body("repeated").Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(RepeatedBinding);
        }

        return Task.CompletedTask;

        IEnumerable<object> Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
