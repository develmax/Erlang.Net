using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedComprehensionScanChecks
{
    internal const string TestName = "compiler/beam/retains-a-lifted-empty-generator-and-its-record-continuation";
    private const string Fixture = "beam_closed_comprehension_scan_probe.erl";
    private const string IncorrectInstructions = "The closed scan or named record continuation emitted the wrong instructions.";
    private const string IncorrectResults = "The closed and open scans returned unexpected list tails.";

    private const string Source = """
        -module(beam_closed_comprehension_scan_probe).
        -export([closed/1, open/2, decode/2]).

        closed(Entries) -> scan(Entries, []).
        open(Entries, Tags) -> open_scan(Entries, Tags).

        scan([], _) -> [];
        scan([{Tag,_}|Tail]=Input, Tags) ->
            case [X || X=T <- Tags, T =:= Tag] of
                [] -> scan(Tail, Tags);
                _ -> Input
            end.

        open_scan([], _) -> [];
        open_scan([{Tag,_}|Tail]=Input, Tags) ->
            case [X || X=T <- Tags, T =:= Tag] of
                [] -> open_scan(Tail, Tags);
                _ -> Input
            end.

        decode(Input, Tags) ->
            List = read(Input, Tags),
            [V0|L1] = List,
            Version = begin Value = read(V0,[1]), rename(Value,[{named,0}]) end,
            [Data0|L2] = L1,
            Data = read(Data0,[2]),
            [More0|L3] = L2,
            More = read(More0,[3]),
            {First,L4} = case L3 of [{4,A}|B] -> {read(A,[]),B}; _ -> {missing,L3} end,
            {Last,L5} = case L4 of [{5,C}|D] -> {read(C,[]),D}; _ -> {missing,L4} end,
            Remaining = scan(L5,[]),
            case Remaining of [] -> true; _ -> exit({error,{asn1,{unexpected,Remaining}}}) end,
            Result = {decoded,Version,Data,More,First,Last},
            Result.

        read(Value, _) -> Value.
        rename(0, _) -> named; rename(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] helper = Read("-scan/2-lc$^0/1-0-");
        object[] scan = Read("scan");
        object[] decode = Read("decode");
        if (helper.Count(instruction => Opcode(instruction) == Return) != 1
            || helper.Any(instruction => Opcode(instruction) == Allocate)
            || Frames(scan) is not [1]
            || scan.Count(instruction => Opcode(instruction) == LocalCall) != 1
            || Frames(decode) is not [6]
            || decode.Any(instruction => Opcode(instruction) == IsNil))
        {
            throw new InvalidOperationException(IncorrectInstructions);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term first = Term.Tuple(Term.I(1), Term.A("first"));
            Term second = Term.Tuple(Term.I(2), Term.A("second"));
            Term third = Term.Tuple(Term.I(3), Term.A("third"));
            Term entries = Term.List(first, second, third);
            Term marker = Term.A("missing");
            Term record = Term.Tuple(
                Term.A("decoded"),
                Term.A("named"),
                first,
                second,
                marker,
                marker
            );
            if (!Nil.Value.Equals(await module.Invoke(context, "closed", entries))
                || !Term.List(second, third).Equals(await module.Invoke(
                    context,
                    "open",
                    entries,
                    Term.List(Term.I(2))
                ))
                || !record.Equals(await module.Invoke(
                    context,
                    "decode",
                    Term.List(
                        Term.I(0),
                        first,
                        second,
                        third
                    ),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectResults);
            }

            return Nil.Value;
        });

        object[] Read(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );

            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectInstructions);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int[] Frames(object[] body) => body.Where(instruction => Opcode(instruction) == Allocate)
        .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
