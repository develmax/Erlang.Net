using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryAndListSchedulingChecks
{
    internal const string TestName = "compiler/beam/redirects-empty-function-clause-relays";
    internal const string FrameTestName = "compiler/beam/folds-short-literal-appends-inside-list-mappers";
    private const string IncorrectFunctionFailure = "A pure failure relay must target the function information label directly.";
    private const string IncorrectListMapper = "The list mapper must build the short literal prefix and trim its saved tail.";
    private const string FailureSource = """
        -module(beam_function_failure_relay_probe).
        -export([connect/3, choose/2, guarded/2]).
        connect(Socket, {local,_}=Address, 0) -> {Socket,Address,0}.
        choose({key,Value}, zero) -> {ok,Value};
        choose({key,_}, one) -> one.
        guarded({key,Value}, 0) ->
            try erlang:element(1, Value)
            catch error:badarg -> no_tuple
            end.
        """;
    private const string MapperSource = """
        -module(beam_mapper_prefix_frame_probe).
        -export([short/1, boundary/1, longer/1, bad_prefix/1]).
        short(List) -> [", " ++ value(Item) || Item <- List].
        boundary(List) -> [[a,b,c,d] ++ value(Item) || Item <- List].
        longer(List) -> [[a,b,c,d,e] ++ value(Item) || Item <- List].
        bad_prefix(List) -> [[a|bad] ++ value(Item) || Item <- List].
        value(boom) -> erlang:error(mapped_failure);
        value(2) -> tail_atom;
        value(Item) -> [Item].
        """;

    internal static Task RunFailureRelay()
    {
        var (instructions, atoms) = Decode(FailureSource, "beam_function_failure_relay_probe.erl");
        int entry = Entry(instructions, atoms, name => name == "connect");
        object failure = instructions.Take(entry).Last(instruction => Opcode(instruction) == Label);
        object comparison = Body(instructions, entry).Single(instruction => Opcode(instruction) == ExactEqualTest);
        if (Value(failure, 0) != Value(comparison, 0)
            || Body(instructions, entry).Any(instruction => Opcode(instruction) == Jump))
        {
            throw new InvalidOperationException(IncorrectFunctionFailure);
        }

        return Task.CompletedTask;
    }

    internal static Task RunMapper()
    {
        var (instructions, atoms) = Decode(MapperSource, "beam_mapper_prefix_frame_probe.erl");
        foreach (string source in new[] { "short", "boundary" })
        {
            object[] body = Mapper(source);
            if (!body.Any(instruction => Opcode(instruction) == Allocate && Value(instruction, 0) == 2)
                || !body.Any(instruction => Opcode(instruction) == TrimFrame && Value(instruction, 0) == 1)
                || body.Any(instruction => Opcode(instruction) == ExternalCall))
            {
                throw new InvalidOperationException(IncorrectListMapper);
            }
        }

        if (!Mapper("longer").Any(instruction => Opcode(instruction) == ExternalCall))
        {
            throw new InvalidOperationException(IncorrectListMapper);
        }

        return Task.CompletedTask;

        object[] Mapper(string name) => Body(
            instructions,
            Entry(instructions, atoms, candidate => candidate.StartsWith($"-{name}/1-lc", StringComparison.Ordinal))
        );
    }

    private static (object[] Instructions, string[] Atoms) Decode(string source, string filename)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, filename));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (instructions, atoms);
    }

    private static int Entry(object[] instructions, string[] atoms, Func<string, bool> matches) =>
        Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && matches(atoms[Value(instruction, 1)]));

    private static object[] Body(object[] instructions, int start) => instructions.Skip(start + 1)
        .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();

    private static int Value(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(index), "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}
