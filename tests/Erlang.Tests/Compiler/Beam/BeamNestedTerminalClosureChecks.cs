using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedTerminalClosureChecks
{
    internal const string TestName = "compiler/beam/builds-nested-terminal-closure-tuples-in-dead-input-registers";
    private const string Fixture = "beam_nested_terminal_closure_probe.erl";
    private const string IncorrectConstruction = "The closure added moves between sibling tuple constructors.";
    private const string IncorrectReason = "The terminal closure changed its captured exception reason.";

    private const string Source = """
        -module(beam_nested_terminal_closure_probe).
        -export([observe/3, make/1]).
        make(Captured) ->
            fun(First, Second, _) ->
                exit({report, {first, First}, {second, Second}, {captured, id, Captured}})
            end.
        observe(Captured, First, Second) ->
            Closure = make(Captured),
            try Closure(First, Second, ignored) of
                Result -> {unexpected, Result}
            catch
                exit:Reason -> {caught, Reason}
            end.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)].StartsWith("-make/", StringComparison.Ordinal) && Value(arity) == 4
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectConstruction);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int[] destinations = body.Where(instruction => Opcode(instruction) == PutTuple)
            .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
        if (destinations is not [0, 1, 2, 0] || body.Any(instruction => Opcode(instruction) == Move))
        {
            throw new InvalidOperationException(IncorrectConstruction);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term captured = Term.A("selected");
            Term first = Term.I(41);
            Term second = Term.A("value");
            Term reason = Term.Tuple(
                Term.A("report"),
                Term.Tuple(Term.A("first"), first),
                Term.Tuple(Term.A("second"), second),
                Term.Tuple(Term.A("captured"), Term.A("id"), captured)
            );
            if (!Term.Tuple(Term.A("caught"), reason).Equals(await module.Invoke(
                context,
                "observe",
                captured,
                first,
                second
            )))
            {
                throw new InvalidOperationException(IncorrectReason);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
