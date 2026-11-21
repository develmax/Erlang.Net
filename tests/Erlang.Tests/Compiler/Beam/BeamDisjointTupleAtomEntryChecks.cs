using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDisjointTupleAtomEntryChecks
{
    internal const string TestName = "compiler/beam/partitions-disjoint-tuple-and-atom-entry-frames";
    private const string WrongLayout = "The disjoint tuple and atom entries must validate their shapes before allocating a frame.";
    private const string WrongRoots = "The constructed outgoing argument must leave only return values in the frame.";
    private const string Fixture = "beam_disjoint_tuple_atom_entry.erl";

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        _ = BeamModule.Prepare(beam);
        object code = Code(beam);
        object function = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Single(candidate => (string)Property(candidate, "Name") == "run");
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        byte[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Skip(first).Take(end - first).Select(Opcode).ToArray();
        int firstTuple = Array.IndexOf(instructions, IsTuple);
        int firstTag = Array.IndexOf(instructions, IsTaggedTuple);
        int firstFrame = Array.FindIndex(instructions, op => op is Allocate or AllocateHeap);
        int firstReturn = Array.IndexOf(instructions, Return);
        if (firstTuple < 0 || firstTag <= firstTuple || firstFrame <= firstTag || firstReturn <= firstFrame)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int atom = Array.IndexOf(instructions, ExactEqualTest, firstReturn + 1);
        int stopTag = Array.IndexOf(instructions, IsTaggedTuple, firstReturn + 1);
        int projection = Array.IndexOf(instructions, GetTupleElement, firstReturn + 1);
        int stopReturn = Array.IndexOf(instructions, Return, firstReturn + 1);
        if (atom < 0 || stopTag <= atom || projection <= stopTag || stopReturn <= projection
            || instructions.AsSpan(atom, stopReturn - atom).Contains(Allocate)
            || instructions.AsSpan(atom, stopReturn - atom).Contains(AllocateHeap))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckReturnRoots(beam, expected: 2);
        string retained = Source.Replace(
            "{bad,Request,State}.",
            "{bad,Request,State,Caller}.",
            StringComparison.Ordinal
        );
        CheckReturnRoots(CodeGeneration.CompileBeamModule(retained, Fixture), expected: 3);

        return Task.CompletedTask;
    }

    private static void CheckReturnRoots(byte[] beam, int expected)
    {
        object code = Code(beam);
        object function = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Single(candidate => (string)Property(candidate, "Name") == "run");
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object? fallback = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Skip(first).Take(end - first).LastOrDefault(instruction => Opcode(instruction) == AllocateHeap);
        if (fallback is null)
        {
            throw new InvalidOperationException(WrongRoots);
        }

        object slots = ((IEnumerable)Property(fallback, "Operands")).Cast<object>().First();
        if ((BigInteger)Property(slots, "Value") != expected)
        {
            throw new InvalidOperationException(WrongRoots);
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private const string Source = """
        -module(beam_disjoint_tuple_atom_entry).
        -export([run/3, overlapping/3]).
        -record(ctx,{owner}).

        run({start,Module,Function,Arguments,Target}, _Caller, State) ->
            Ref = entry_provider:pin(Target),
            Key = {entry,Ref},
            Value = {binding,Module,Function,Arguments,Target},
            entry_provider:store(Key,Value),
            {reply,Ref,State};
        run(stop,{Owner,_Tag},State=#ctx{owner=Owner}) ->
            {stop,normal,State};
        run(Request,Caller,State) ->
            entry_provider:warning([Caller,Request]),
            {bad,Request,State}.

        overlapping({start,Value}, Caller, State) when Value =:= Caller ->
            {accepted,State};
        overlapping({start,Value}, _Caller, State) ->
            entry_provider:remember(Value),
            {value,State};
        overlapping(stop,{Owner,_Tag},State=#ctx{owner=Owner}) ->
            {stop,normal,State};
        overlapping(Request,Caller,State) ->
            entry_provider:warning([Caller,Request]),
            {bad,Request,State}.
        """;
}
