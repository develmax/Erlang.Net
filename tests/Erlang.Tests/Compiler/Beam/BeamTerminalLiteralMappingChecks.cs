using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalLiteralMappingChecks
{
    internal const string TestName = "compiler/beam/groups-terminal-atom-to-literal-mappings";
    private const string Fixture = "beam_terminal_literal_mapping.erl";
    private const string WrongTable = "The terminal literal mapping lost its ordered selector or shared result.";
    private const string WrongResult = "The terminal mapping changed a fallback, guard or literal result.";

    private const string Source = """
        -module(beam_terminal_literal_mapping).
        -export([convert/1, render/2, case_map/2, guarded/2, two_columns/2, fallback_effect/0]).

        convert(south) -> 10;
        convert(east) -> 4;
        convert(north) -> 8;
        convert(_) -> erlang:error(unknown_cardinal).

        render(zulu, _) -> "z";
        render(alpha, _) -> "a";
        render(pi, _) -> "p";
        render(Key, Context) -> fallback(Key, Context).

        fallback(Key, Context) ->
            erlang:put(fallback_called, {Key, Context}),
            {Key, Context}.

        fallback_effect() -> erlang:get(fallback_called).

        case_map(Key, _) ->
            case Key of
                cedar -> ["shared"];
                beta -> [];
                delta -> ["distinct", {entry, 4}];
                alpha -> ["shared"]
            end.

        guarded(Key, Flag) ->
            case Key of
                alpha when Flag =:= true -> "g";
                beta -> "b"
            end.

        two_columns(alpha, primary) -> 1;
        two_columns(beta, secondary) -> 2;
        two_columns(_, _) -> erlang:error(wrong_column).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("convert", ["east", "north", "south"], expectedReturns: 3);
        Check("render", ["alpha", "pi", "zulu"], expectedReturns: 3);
        Check("case_map", ["alpha", "beta", "cedar", "delta"], expectedReturns: 3);
        if (!Body("case_map").Any(instruction => Opcode(instruction) == CaseEnd)
            || !Body("two_columns").Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongTable);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term source = Term.A("west");
            Term state = Term.A("kept");
            Term fallback = Term.Tuple(source, state);
            if (!Term.I(10).Equals(await module.Invoke(context, "convert", Term.A("south")))
                || !Term.List(Term.I('p')).Equals(await module.Invoke(
                    context,
                    "render",
                    Term.A("pi"),
                    state
                ))
                || !Term.A("undefined").Equals(await module.Invoke(context, "fallback_effect"))
                || !fallback.Equals(await module.Invoke(
                    context,
                    "render",
                    source,
                    state
                ))
                || !fallback.Equals(await module.Invoke(context, "fallback_effect"))
                || !Nil.Value.Equals(await module.Invoke(
                    context,
                    "case_map",
                    Term.A("beta"),
                    state
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "guarded",
                    Term.A("alpha"),
                    Term.A("false")
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), Term.A("alpha"))))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });

        void Check(string name, string[] keys, int expectedReturns)
        {
            object[] body = Body(name);
            object? select = body.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
            if (select is null || body.Count(instruction => Opcode(instruction) == Return) != expectedReturns)
            {
                throw new InvalidOperationException(WrongTable);
            }

            object[] operands = Operands(select);
            var encoded = operands.Skip(3).Where((_, index) => index % 2 == 0)
                .Select(operand => atoms[Value(operand)]);
            if (!encoded.SequenceEqual(keys) || Value(operands[0]) != 0)
            {
                throw new InvalidOperationException(WrongTable);
            }

            if (name == "case_map" && Value(operands[4]) != Value(operands[8]))
            {
                throw new InvalidOperationException(WrongTable);
            }
        }

        object[] Body(string name)
        {
            int index = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name);
            if (index < 0)
            {
                throw new InvalidOperationException(WrongTable);
            }

            return instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
