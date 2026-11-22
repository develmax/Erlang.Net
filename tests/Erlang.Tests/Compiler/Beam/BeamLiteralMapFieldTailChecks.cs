using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralMapFieldTailChecks
{
    internal const string TestName = "compiler/beam/keeps-literal-map-entry-and-tail-call-frameless";
    private const string WrongLayout = "Expected the map and literal check to preserve the original input registers.";
    private const string WrongBehavior = "The literal map entry changed its result or function clause failure.";
    private const string Source = """
        -module(beam_literal_map_field_tail).
        -export([lookup/2, fallback/2, repeated/3]).

        lookup(Key, #{mode := selected} = Map) ->
            maps:get(Key, Map, absent).

        fallback(Key, Map = #{mode := selected}) ->
            maps:get(Key, Map, absent);
        fallback(_, _) -> missed.

        repeated(Key, #{mode := selected} = Map, Key) ->
            maps:get(Key, Map, absent).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_literal_map_field_tail.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, operation => Opcode(operation) == FunctionInfo
            && atoms[Value(Items(operation, "Operands")[1])] == "lookup");
        if (info <= 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int infoLabel = info - 1;
        while (infoLabel >= 0 && Opcode(instructions[infoLabel]) == Line)
        {
            infoLabel--;
        }

        if (infoLabel < 0 || Opcode(instructions[infoLabel]) != Label)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 2).Take(6).ToArray();
        byte[] expected = [IsMap, GetMapElements, ExactEqualTest, Move, Line, TailExternalCall];
        if (!expected.SequenceEqual(body.Select(Opcode)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int rejected = Value(Items(instructions[infoLabel], "Operands")[0]);
        object[] extraction = Items(body[1], "Operands");
        object[] literal = Items(body[2], "Operands");
        if (Value(Items(body[0], "Operands")[0]) != rejected || Value(extraction[0]) != rejected
            || Value(literal[0]) != rejected || Value(extraction[1]) != 1 || Value(extraction[^1]) != 2
            || Value(literal[1]) != 2 || atoms[Value(literal[^1])] != "selected")
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("payload");
            Term selected = new MapTerm([new(Term.A("mode"), Term.A("selected")), new(key, Term.I(29))]);
            Term wrong = new MapTerm([new(Term.A("mode"), Term.I(1))]);
            if (!Term.I(29).Equals(await module.Invoke(
                context,
                "lookup",
                key,
                selected
            ))
                || !Term.A("absent").Equals(await module.Invoke(
                    context,
                    "lookup",
                    Term.A("missing"),
                    selected
                ))
                || !Term.A("missed").Equals(await module.Invoke(
                    context,
                    "fallback",
                    key,
                    wrong
                ))
                || !Term.I(29).Equals(await module.Invoke(
                    context,
                    "repeated",
                    key,
                    selected,
                    key
                )))
            {
                throw new InvalidOperationException(WrongBehavior);
            }

            await CheckFailure("lookup", [key, wrong]);
            await CheckFailure("lookup", [key, new MapTerm([])]);
            await CheckFailure("lookup", [key, Term.A("non_map")]);
            await CheckFailure("repeated", [key, selected, Term.A("different")]);

            return Nil.Value;

            async Task CheckFailure(string function, Term[] arguments)
            {
                try
                {
                    _ = await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongBehavior);
            }
        });
    }

    private static object[] Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
