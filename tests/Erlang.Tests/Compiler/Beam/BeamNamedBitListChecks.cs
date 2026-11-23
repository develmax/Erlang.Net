using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedBitListChecks
{
    internal const string PackTestName = "compiler/beam/keeps-the-bit-list-tail-when-initializing-the-octet-count";
    internal const string LookupTestName = "compiler/beam/lowers-tagged-bit-positions-and-missing-names";
    internal const string ContinuationTestName = "compiler/beam/proves-positive-octet-count-through-recursive-packing";
    private const string Fixture = "beam_named_bit_list_probe.erl";
    private const string PackExport = "encode";
    private const string LookupExport = "positions";
    private const string PackHelper = "pack";
    private const string LookupHelper = "lookup";
    private const string XRegisterKind = "x";
    private const string IntegerKind = "integer";
    private const string PackLayout = "The octet packer did not preserve its live list tail before assigning the count.";
    private const string PackValue = "The octet packer changed the byte boundary, trailing bit or short-list fallback.";
    private const string LookupLayout = "The position loop did not test tagged entries and atoms before allocating the lookup frame.";
    private const string ContinuationLayout = "Only the closed bit-list continuation should pack another octet without a frame.";
    private const string ContinuationValue = "The octet continuation changed its result or open-entry fallback.";
    private const string LookupValue = "The position loop changed lookup, explicit positions or malformed input handling.";

    private const string Source = """
        -module(beam_named_bit_list_probe).
        -export([encode/1, positions/1]).

        encode(second_byte) -> pack([1,0,0,0,0,0,0,0,0,1]);
        encode(byte_boundary) -> pack([1,0,0,0,0,0,0,1]);
        encode(short) -> pack([0,1]);
        encode(two_bytes) -> pack([1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1]);
        encode(three_bytes) -> pack([1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,1]).

        pack([B7,B6,B5,B4,B3,B2,B1,B0 | Rest]) ->
            Octet = (B7 bsl 7) bor (B6 bsl 6) bor (B5 bsl 5) bor
                (B4 bsl 4) bor (B3 bsl 3) bor (B2 bsl 2) bor (B1 bsl 1) bor B0,
            pack(Rest, [Octet], 1);
        pack(Rest) ->
            {Unused, Last} = pack_tail(Rest, 7, 0),
            {1, Unused, [Last]}.

        pack([B7,B6,B5,B4,B3,B2,B1,B0 | Rest], Octets, Count) ->
            Octet = (B7 bsl 7) bor (B6 bsl 6) bor (B5 bsl 5) bor
                (B4 bsl 4) bor (B3 bsl 3) bor (B2 bsl 2) bor (B1 bsl 1) bor B0,
            pack(Rest, [Octets, Octet], Count + 1);
        pack([], Octets, Count) ->
            {Count, 0, Octets};
        pack(Rest, Octets, Count) ->
            {Unused, Last} = pack_tail(Rest, 7, 0),
            {Count + 1, Unused, [Octets, Last]}.

        pack_tail([], Shift, Accumulator) ->
            {Shift + 1, Accumulator};
        pack_tail([Bit | Rest], Shift, Accumulator) ->
            pack_tail(Rest, Shift - 1, (Bit bsl Shift) bor Accumulator);
        pack_tail(_, _, _) ->
            {8, 0}.

        positions(Bits) ->
            lookup(Bits, [{low,0},{high,9}], []).

        lookup([{bit, Position} | Tail], Names, Positions) ->
            lookup(Tail, Names, [Position | Positions]);
        lookup([Name | Tail], Names, Positions) when is_atom(Name) ->
            case lists:keyfind(Name, 1, Names) of
                {_, Position} ->
                    lookup(Tail, Names, [Position | Positions]);
                _ ->
                    exit({error, {position, {unknown, Name}}})
            end;
        lookup([], _, Positions) ->
            lists:sort(Positions).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task RunPack()
    {
        byte[] image = CompileFixture();
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            (string Input, Term Expected)[] samples =
            [
                ("second_byte", Term.Tuple(Term.I(2), Term.I(6), Term.List(Term.List(Term.I(128)), Term.I(64)))),
                ("byte_boundary", Term.Tuple(Term.I(1), Term.I(0), Term.List(Term.I(129)))),
                ("short", Term.Tuple(Term.I(1), Term.I(6), Term.List(Term.I(64))))
            ];
            foreach (var (input, expected) in samples)
            {
                if (!expected.Equals(await module.Invoke(context, PackExport, Term.A(input))))
                {
                    throw new InvalidOperationException(PackValue);
                }
            }

            return Nil.Value;
        });

        object[] instructions = Body(
            image,
            PackHelper,
            1,
            PackLayout
        );
        int transfer = Array.FindIndex(instructions, instruction => IsMove(
            instruction,
            XRegisterKind,
            2,
            0
        ));
        int count = Array.FindIndex(instructions, instruction => IsMove(
            instruction,
            IntegerKind,
            1,
            2
        ));
        if (transfer < 0 || count != transfer + 1 || count + 1 >= instructions.Length
            || Opcode(instructions[count + 1]) != TailLocalCall)
        {
            throw new InvalidOperationException(PackLayout);
        }
    }

    internal static async Task RunLookup()
    {
        byte[] image = CompileFixture();
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term known = Term.A("high");
            Term missing = Term.A("missing");
            Term explicitPosition = Term.Tuple(Term.A("bit"), Term.I(0));
            Term expected = Term.List(Term.I(0), Term.I(9));
            if (!expected.Equals(await module.Invoke(
                context,
                LookupExport,
                Term.List(known, explicitPosition)
            )))
            {
                throw new InvalidOperationException(LookupValue);
            }

            Term reason = Term.Tuple(Term.A("error"), Term.Tuple(Term.A("position"), Term.Tuple(Term.A("unknown"), missing)));
            try
            {
                _ = await module.Invoke(context, LookupExport, Term.List(known, missing));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit && error.Reason.Equals(reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(LookupValue);
        });

        object[] selectAndAllocate = Body(
            image,
            LookupHelper,
            3,
            LookupLayout
        )
            .Where(instruction => Opcode(instruction) is IsTaggedTuple or IsAtom or Allocate or AllocateHeap).ToArray();
        const int liveLookupInputs = 5;
        const int savedLookupSlots = 4;
        if (selectAndAllocate.Select(Opcode).ToArray() is not [IsTaggedTuple, IsAtom, Allocate]
            || Items(selectAndAllocate[2], "Operands").Select(Value).ToArray() is not [savedLookupSlots, liveLookupInputs])
        {
            throw new InvalidOperationException(LookupLayout);
        }
    }

    internal static async Task RunContinuation()
    {
        byte[] closedImage = CompileFixture();
        const string closedExports = "-export([encode/1, positions/1]).";
        const string openExports = "-export([encode/1, positions/1, pack/3]).";
        byte[] openImage = CodeGeneration.CompileBeamModule(Source.Replace(closedExports, openExports), Fixture);
        var closed = BeamModule.Prepare(closedImage);
        var open = BeamModule.Prepare(openImage);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term encoded = Term.List(Term.List(Term.I(128)), Term.I(65));
            Term withTail = Term.List(encoded, Term.I(64));
            if (!Term.Tuple(Term.I(2), Term.I(0), encoded).Equals(
                await closed.Invoke(context, PackExport, Term.A("two_bytes")))
                || !Term.Tuple(Term.I(3), Term.I(6), withTail).Equals(
                    await closed.Invoke(context, PackExport, Term.A("three_bytes"))))
            {
                throw new InvalidOperationException(ContinuationValue);
            }

            Term seed = Term.A("seed");
            Term expected = Term.Tuple(Term.I(-1), Term.I(0), Term.List(seed, Term.I(256)));
            Term arbitraryDigits = Term.List(
                Term.I(2),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0),
                Term.I(0)
            );
            if (!expected.Equals(await open.Invoke(
                context,
                PackHelper,
                arbitraryDigits,
                seed,
                Term.I(-2)
            )))
            {
                throw new InvalidOperationException(ContinuationValue);
            }

            return Nil.Value;
        });

        byte[] closedBody = Body(
            closedImage,
            PackHelper,
            3,
            ContinuationLayout
        ).Select(Opcode).ToArray();
        byte[] openBody = Body(
            openImage,
            PackHelper,
            3,
            ContinuationLayout
        ).Select(Opcode).ToArray();
        int firstRecursiveCall = Array.IndexOf(closedBody, TailLocalCall);
        int firstFrame = Array.IndexOf(closedBody, Allocate);
        if (firstRecursiveCall < 0 || firstFrame < firstRecursiveCall
            || openBody.Contains(TailLocalCall)
            || !openBody.Contains(TailLocalCallWithFrame))
        {
            throw new InvalidOperationException(ContinuationLayout);
        }
    }

    private static object[] Body(
        byte[] image,
        string function,
        int arity,
        string failure
    )
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var count]
                && atoms[Value(name)] == function && Value(count) == arity
        );
        if (start < 0)
        {
            throw new InvalidOperationException(failure);
        }

        return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static bool IsMove(
        object instruction,
        string sourceKind,
        int from,
        int to
    ) =>
        Opcode(instruction) == Move && Items(instruction, "Operands") is [var source, var destination]
        && (string)Property(source, "Kind") == sourceKind && Value(source) == from
        && (string)Property(destination, "Kind") == XRegisterKind && Value(destination) == to;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
