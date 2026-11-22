using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedDelimitersChecks
{
    internal const string TestName = "compiler/beam/positions-prefix-retries-in-delimiter-scanner";
    private const string WrongSchedule = "The delimiter scanner lost its owned match context or leaf frames.";
    private const string WrongResult = "The delimiter scanner changed a prefix retry, continuation, or consumed suffix.";
    private const string Source = """
        -module(beam_positioned_delimiters).
        -export([feed/4]).

        feed(Pending, Data, Header, Headers) -> scan(<<Pending/binary, Data/binary>>, Header, Headers).

        scan(<<>>, H, Hs) -> {?MODULE, feed, [<<>>, H, Hs]};
        scan(<<126, 11>>, H, Hs) -> {?MODULE, feed, [<<126, 11>>, H, Hs]};
        scan(<<11>>, H, Hs) -> {?MODULE, feed, [<<11>>, H, Hs]};
        scan(<<126, 11, 126, 11, Tail/binary>>, H, Hs) -> {done, {[lists:reverse([11, 126 | H]) | Hs], Tail}};
        scan(<<11, 11, Tail/binary>>, H, Hs) -> {done, {[lists:reverse([11 | H]) | Hs], Tail}};
        scan(<<126, 11, Tail/binary>>, H, Hs) -> scan(Tail, [], [lists:reverse([11, 126 | H]) | Hs]);
        scan(<<11, Tail/binary>>, H, Hs) -> scan(Tail, [], [lists:reverse([11 | H]) | Hs]);
        scan(<<Octet, Tail/binary>>, H, Hs) -> scan(Tail, [Octet | H], Hs).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_positioned_delimiters.erl");
        CheckLayout(image, optimized: true);
        string disabled = Source.Replace("-export", "-compile(no_ssa_opt).\n-export", StringComparison.Ordinal);
        CheckLayout(CodeGeneration.CompileBeamModule(disabled, "beam_positioned_delimiters.erl"), optimized: false);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([ListModuleNames.Module]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed(ErlangModuleNames.Module), RuntimeModuleSelection.Managed(ListModuleNames.Module)]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var empty = new BitString([]);
            Term invalidHeader = Term.A("invalid_header");
            await Check(new BitString([]), Continuation(empty, Nil.Value));
            await Check(new BitString([126, 11]), Continuation(new BitString([126, 11]), Nil.Value));
            await Check(new BitString([126, 255, 11]), Continuation(new BitString([11]), Term.List(Term.I(255), Term.I(126))));
            await Check(
                new BitString([97, 126, 11, 126, 11, 255]),
                Done(Term.List(Term.List(Term.I(97), Term.I(126), Term.I(11))), new BitString([255]))
            );
            await Check(
                new BitString([97, 126, 11, 98, 11, 11, 0]),
                Done(
                    Term.List(Term.List(Term.I(98), Term.I(11)), Term.List(Term.I(97), Term.I(126), Term.I(11))),
                    new BitString([0])
                )
            );
            await Check(new BitString([11]), Continuation(new BitString([11]), invalidHeader), invalidHeader);

            return Nil.Value;

            async Task Check(BitString input, Term expected, Term? header = null)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    "feed",
                    empty,
                    input,
                    header ?? Nil.Value,
                    Nil.Value
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }
        });
    }

    private static Term Continuation(BitString prefix, Term header) =>
        Term.Tuple(Term.A("beam_positioned_delimiters"), Term.A("feed"), Term.List(prefix, header, Nil.Value));

    private static Term Done(Term lines, BitString tail) => Term.Tuple(Term.A("done"), Term.Tuple(lines, tail));

    private static void CheckLayout(byte[] image, bool optimized)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Operand(instruction, 1)] == "scan");
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        bool shared = body.Count(instruction => Opcode(instruction) == StartBinaryMatchKnown) == 1
            && body.Count(instruction => Opcode(instruction) == GetBinaryPosition) == 3
            && body.Count(instruction => Opcode(instruction) == SetBinaryPosition) == 4
            && body.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap)
                .Select(instruction => Operand(instruction, 0)).SequenceEqual([2, 3, 2, 3]);
        if (shared != optimized)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        if (optimized)
        {
            CheckCompactEqualityCommands(code, body, atoms);
        }
    }

    private static void CheckCompactEqualityCommands(object code, object[] body, string[] atoms)
    {
        const int commandCount = 7;
        const int equalityByte = 11;
        const int equalityCommands = 2;
        object header = Property(Property(code, "Structure"), "Code");
        var instructions = (ReadOnlyMemory<byte>)Property(header, "Instructions");
        var compact = Type("BeamCompactReader");
        int found = 0;
        for (int index = 0; index + 1 < body.Length; index++)
        {
            object instruction = body[index];
            if (Opcode(instruction) != BinaryMatch)
            {
                continue;
            }

            object[] operands = Items(instruction, "Operands");
            if (operands.Length < 7 || (string)Property(operands[6], "Kind") != "atom"
                || atoms[Operand(instruction, 6)] != ErlangOperators.ExactEqual)
            {
                continue;
            }

            found++;
            if (operands.Length != commandCount + 3 || Operand(instruction, 2) != commandCount
                || Operand(instruction, 8) != 8 || Operand(instruction, 9) != equalityByte)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            int start = (int)Property(instruction, "Offset") + 1;
            object cursor = New("BeamCursor", instructions[start..]);
            _ = Call(compact, "Read", cursor);
            _ = Call(compact, "Read", cursor);
            _ = Call(compact, "Read", cursor);
            object count = Call(compact, "Read", cursor);
            for (int operand = 0; operand < commandCount - 1; operand++)
            {
                _ = Call(compact, "Read", cursor);
            }

            object expectedByte = Call(compact, "Read", cursor);
            if ((BigInteger)Property(count, "Value") != commandCount
                || (BigInteger)Property(expectedByte, "Value") != equalityByte
                || start + (int)Property(cursor, "Position") != (int)Property(body[index + 1], "Offset"))
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }

        if (found != equalityCommands)
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static int Operand(object instruction, int index) =>
        (int)(BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
