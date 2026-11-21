using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListGeneratorChecks
{
    internal const string TestName = "compiler/beam/threads-two-list-generators-through-owned-recursive-helpers";
    private const string WrongTopology = "The outer and inner list generators must preserve their separate continuation and frame.";
    private const string WrongFailureLocation = "The nested list generator error must retain the generator input line.";
    private const string Fixture = "beam_nested_generator_topology.erl";
    private const string Source = """
        -module(beam_nested_generator_topology).
        -export([pairs/2, overlap/2]).
        pairs(Left,Right) -> [{X,Y} || X <- Left, Y <- Right].
        overlap(Left,Right) -> [X || X <- Left, Y <- Right, X =:= Y].
        """;

    private const string ComputedSource = """
        -module(beam_computed_outer_generators).
        -export([joined/2, each/2, mark/2]).

        joined(Rows, Lookup) ->
            [{Row, Item} || Row <- mark(outer, Rows),
                            Item <- mark(Row, maps:get(Row, Lookup, []))].

        each(Rows, Items) ->
            [{Row, Item} || Row <- ?MODULE:mark(outer, Rows), Item <- Items].

        mark(Event, Value) ->
            self() ! {observed, Event},
            Value.
        """;

    private const string SplitComputedSource = """
        -module(beam_split_computed_generators).
        -export([split/2, mark/2]).
        split(Rows, Lookup) ->
            [{Row, Item} || Row <-
                mark(outer, Rows),
                            Item <-
                mark(Row, maps:get(Row, Lookup, []))].
        mark(_Value, Result) ->
            Result.
        """;

    private const string MixedSource = """
        -module(nested_gen_entry_probe).
        -export([rule/1, report/1]).

        report(Errors) when is_list(Errors) ->
            _ = [write_error(File, Module, Reason) ||
                 {structured_error,File,Module,Reason} <- Errors],
            ok.

        write_error(File, Module, Reason) ->
            erlang:put(last_nested_generator_error,{File,Module,Reason}).

        rule(Options) ->
            case [Rule || Rule <- [ber,per,uper,jer],
                          Opt <- Options,
                          Rule =:= Opt] of
                [] -> ber;
                [Mode|_] -> Mode
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Verify("pairs", 2, hasFilter: false);
        Verify("overlap", 1, hasFilter: true);
        _ = Code(CodeGeneration.CompileBeamModule(MixedSource, "nested_gen_entry_probe.erl"));
        VerifyComputedSources();

        return Task.CompletedTask;

        void Verify(string name, int frame, bool hasFilter)
        {
            object[] outer = Body($"-{name}/2-lc$^0/1-0-", 2);
            object[] inner = Body($"-{name}/2-lc$^1/1-1-", 4);
            if (!outer.Any(i => Opcode(i) == TailLocalCall && Value(i, 0) == 4)
                || !inner.Any(i => Opcode(i) == Allocate && Value(i, 0) == frame)
                || !inner.Any(i => Opcode(i) == TailLocalCall && Value(i, 0) == 2)
                || inner.Any(i => Opcode(i) == ExactEqualTest) != hasFilter
                || inner.TakeWhile(i => Opcode(i) != Deallocate).Any(i => Opcode(i) == PutTuple) == hasFilter)
            {
                throw new InvalidOperationException(WrongTopology);
            }
        }

        object[] Body(string name, int arity)
        {
            int info = Array.FindIndex(
                instructions,
                i => Opcode(i) == FunctionInfo && atoms[Value(i, 1)] == name && Value(i, 2) == arity
            );
            if (info < 0)
            {
                throw new InvalidOperationException(WrongTopology);
            }

            return instructions.Skip(info + 1).TakeWhile(i => Opcode(i) != FunctionInfo).ToArray();
        }
    }

    private static void VerifyComputedSources()
    {
        object code = Code(CodeGeneration.CompileBeamModule(ComputedSource, "beam_computed_outer_generators.erl"));
        var helpers = Items(code, "Functions")
            .OrderBy(function => (int)Property(function, "FirstInstructionIndex"))
            .Select(function => (Name: (string)Property(function, "Name"), Arity: (int)Property(function, "Arity")))
            .Where(function => function.Name.Contains("-lc$^", StringComparison.Ordinal));
        if (!helpers.SequenceEqual([
                ("-each/2-lc$^0/1-0-", 2),
                ("-each/2-lc$^1/1-1-", 4),
                ("-joined/2-lc$^0/1-0-", 2),
                ("-joined/2-lc$^1/1-1-", 4)
            ]))
        {
            throw new InvalidOperationException(WrongTopology);
        }

        VerifyGeneratorFailureLine(code, "-joined/2-lc$^0/1-0-", 5);
        object split = Code(CodeGeneration.CompileBeamModule(SplitComputedSource, "beam_split_computed_generators.erl"));
        VerifyGeneratorFailureLine(split, "-split/2-lc$^0/1-0-", 5);
        VerifyGeneratorFailureLine(split, "-split/2-lc$^1/1-1-", 7);
    }

    private static void VerifyGeneratorFailureLine(object code, string name, int line)
    {
        object function = Items(code, "Functions").Single(candidate => (string)Property(candidate, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").ToArray()[start..end];
        if (body.Length < 3 || Opcode(body[^3]) != PutTuple || Opcode(body[^2]) != Line
            || Opcode(body[^1]) != TailExternalCall)
        {
            throw new InvalidOperationException(WrongFailureLocation);
        }

        var location = Items(code, "SourceLocations").Cast<Term>().ElementAt(Value(body[^2], 0));
        if (!Cons.Items(location).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
        {
            throw new InvalidOperationException(WrongFailureLocation);
        }
    }

    private static int Value(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(index), "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}
