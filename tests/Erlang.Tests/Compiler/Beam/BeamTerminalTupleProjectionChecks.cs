using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalTupleProjectionChecks
{
    internal const string TestName = "compiler/beam/projects-checked-fields-directly-into-terminal-registers";
    private const string Fixture = "beam_terminal_projection_probe.erl";
    private const string WrongLayout = "The checked field used a wrong terminal register.";
    private const string WrongResult = "The terminal projection changed an observable value.";
    private const string Source = """
        -module(beam_terminal_projection_probe).
        -export([select_location/2, select_call/3, duplicate/3, original_source/2]).

        select_location({Line, _}, line) -> Line;
        select_location(Location, _) -> Location.

        select_call(Sink, {Key, Value}, Key) -> erlang:put(Sink, Value);
        select_call(Sink, Pair, _) -> erlang:put(Sink, Pair).

        duplicate({Key, Value}, Expected, Keep) when Key =:= Expected -> finish(Value, Value, Keep);
        duplicate(Value, _, Keep) -> finish(Value, other, Keep).

        original_source({Part, _} = Original, first) -> {Original, Part};
        original_source(Original, _) -> {Original, original}.

        finish(A, B, C) -> {A, B, C}.
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
            Term first = Term.A("first");
            Term value = Term.A("value");
            Term other = Term.A("other");
            Term sink = Term.A("sink");
            Term pair = Term.Tuple(first, value);
            if (!first.Equals(await module.Invoke(
                context,
                "select_location",
                pair,
                Term.A("line")
            ))
                || !pair.Equals(await module.Invoke(
                    context,
                    "select_location",
                    pair,
                    other
                ))
                || !Term.A("undefined").Equals(await module.Invoke(
                    context,
                    "select_call",
                    sink,
                    pair,
                    first
                ))
                || !value.Equals(await module.Invoke(
                    context,
                    "select_call",
                    sink,
                    pair,
                    other
                ))
                || !pair.Equals(await module.Invoke(
                    context,
                    "select_call",
                    sink,
                    pair,
                    first
                ))
                || !Term.Tuple(value, value, other).Equals(await module.Invoke(
                    context,
                    "duplicate",
                    pair,
                    first,
                    other
                ))
                || !Term.Tuple(pair, first).Equals(await module.Invoke(
                    context,
                    "original_source",
                    pair,
                    first
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayouts(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!ProjectsDirectly("select_location", 0, Return)
            || !ProjectsDirectly("select_call", 1, TailExternalCall)
            || !Body("duplicate").Any(instruction => Opcode(instruction) == Move))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        bool ProjectsDirectly(string name, int register, byte terminal)
        {
            object[] body = Body(name);
            int projection = Array.FindLastIndex(body, instruction => Opcode(instruction) == GetTupleElement);
            if (projection < 0)
            {
                return false;
            }

            // A call line can occur between the projected value and its consumer.
            object next = body.Skip(projection + 1).First(instruction => Opcode(instruction) != Line);

            return Opcode(next) == terminal && Value(Children(body[projection], "Operands").Last()) == register;
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
