using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInclusiveSizeBoundsChecks
{
    internal const string TestName = "compiler/beam/inclusive-size-guard-fallthrough";
    private const string Fixture = "beam_inclusive_size_bounds_probe.erl";
    private const string WrongBounds = "The second guard did not keep the proven lower bound along its fallthrough edge.";
    private const string WrongResult = "The size guard changed its result or failure on a boundary.";
    private const string Source = """
        -module(beam_inclusive_size_bounds_probe).
        -export([bounded/2, joined/1]).
        bounded(Value, Tag) ->
            begin
                Binary = input(Value, Tag),
                Size = byte_size(Binary),
                if 3 =< Size, Size =< 5 -> Binary;
                   true -> exit(out_of_range)
                end
            end.
        joined(Value) ->
            Size = byte_size(Value),
            Branch = if 2 =< Size -> lower; true -> smaller end,
            if Size =< 5 -> Branch; true -> larger end.
        input(Value, pass) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckBounds(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (int size in new[] { 2, 3, 5, 6 })
            {
                Term binary = new BitString(new byte[size]);
                try
                {
                    Term actual = await module.Invoke(
                        context,
                        "bounded",
                        binary,
                        Term.A("pass")
                    );
                    if (size is 2 or 6 || !actual.Equals(binary))
                    {
                        throw new InvalidOperationException(WrongResult);
                    }
                }
                catch (ErlangException error) when (size is 2 or 6 && error.ExceptionClass == ErlangExceptionClasses.Exit
                    && error.Reason.Equals(Term.A("out_of_range")))
                {
                }
            }

            if (!Term.A("smaller").Equals(await module.Invoke(context, "joined", new BitString(new byte[1]))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckBounds(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        object[] types = Children(Property(code, "Types"), "Entries");
        var atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[][] bounded = Tests("bounded");
        if (bounded.Length != 2
            || Property(bounded[0][1], "TypeIndex") is not int lower
            || Property(bounded[1][2], "TypeIndex") is not int upper
            || (long)Property(types[lower], "Minimum") != 0
            || (long)Property(types[upper], "Minimum") != 3
            || (long)Property(types[lower], "Maximum") != (long)Property(types[upper], "Maximum")
            || Tests("joined").SelectMany(operands => operands)
                .Any(operand => Property(operand, "TypeIndex") is int index && (long)Property(types[index], "Minimum") == 2))
        {
            throw new InvalidOperationException(WrongBounds);
        }

        object[][] Tests(string name)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Children(item, "Operands")[1], "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Where(item => Opcode(item) == IsGreaterOrEqual).Select(item => Children(item, "Operands")).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
