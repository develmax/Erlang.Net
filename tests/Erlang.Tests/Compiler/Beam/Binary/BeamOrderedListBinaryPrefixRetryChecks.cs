using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamOrderedListBinaryPrefixRetryChecks
{
    internal const string TestName = "compiler/beam/reuses-cursor-for-ordered-binary-list-prefix-retry";
    private const string WrongCursor = "The ordered binary prefixes lost their shared cursor or raw fallback.";
    private const string SourceFile = "beam_prefix_retries_probe.erl";
    private const string Source = """
        -module(beam_prefix_retries_probe).
        -export([walk/2]).
        walk([<<"012345678", _/binary>>|Tail], Acc) ->
            {lists:reverse(Acc),Tail};
        walk([<<"012345678beta-longrest", _/binary>>|Tail], Acc) ->
            {lists:reverse(Acc),Tail};
        walk([Head|Tail], Acc) ->
            walk(Tail, [Head|Acc]).
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, SourceFile);
        object[] instructions = Read(Code(image), "Instructions").ToArray();
        object[] matches = instructions.Where(instruction => Opcode(instruction) == MatchBinaryString).ToArray();
        if (instructions.Count(instruction => Opcode(instruction) == StartBinaryMatch) != 1
            || matches.Length != 2
            || matches.Any(match => Operand(match, 1) != 3)
            || Operand(matches[0], 2) != 72
            || Operand(matches[1], 2) != 88
            || !instructions.Any(instruction => Opcode(instruction) == TailLocalCall && Operand(instruction, 0) == 2))
        {
            throw new InvalidOperationException(WrongCursor);
        }

        var compiler = new BeamModuleCompiler("beam_checked_token_retry_fixture");
        _ = Call(compiler, "Intern", "run");
        _ = Call(compiler, "Intern", "long");
        _ = Call(compiler, "Intern", "checked");
        if (Rewrite(TokenFixture()) is not { } shared || !shared.SequenceEqual(TokenFixture(shared: true))
            || Rewrite(shared) is not null || Rewrite(TokenFixture(alternateEntry: true)) is not null
            || Rewrite(TokenFixture(differentField: true)) is not null)
        {
            throw new InvalidOperationException(WrongCursor);
        }

        var arityCompiler = new BeamModuleCompiler("beam_checked_list_retry_arity_fixture");
        _ = Call(arityCompiler, "Intern", "run");
        _ = Call(arityCompiler, "Intern", "tag_z");
        _ = Call(arityCompiler, "Intern", "tag_a");
        _ = Call(arityCompiler, "Intern", "value");
        if (Partition(ArityFixture()) is not { Code: var selected, LabelCount: 7 }
            || !selected.Span.SequenceEqual(ArityFixture(selected: true))
            || Partition(selected, 7) is not null || Partition(ArityFixture(otherEntry: true)) is not null
            || Partition(ArityFixture(liveFallback: true)) is not null
            || Partition(ArityFixture(otherRetryTag: true)) is not null
            || Partition(ArityFixture(delimiter: true)) is not { Code: var delimited, LabelCount: 8 }
            || !delimited.Span.SequenceEqual(ArityFixture(selected: true, delimiter: true))
            || Partition(delimited, 8) is not null
            || Partition(ArityFixture(delimiter: true, unequalSharedTag: true)) is not
            { Code: var unshared, LabelCount: 7 }
            || !unshared.Span.SequenceEqual(ArityFixture(
                selected: true,
                delimiter: true,
                unequalSharedTag: true
            )))
        {
            throw new InvalidOperationException(WrongCursor);
        }

        return Task.CompletedTask;

        (ReadOnlyMemory<byte> Code, int LabelCount)? Partition(ReadOnlyMemory<byte> code, int labels = 6) =>
            ((ReadOnlyMemory<byte>, int)?)Call(
                arityCompiler,
                "SelectCheckedListRetryArities",
                code,
                labels
            );

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ShareCheckedTokenRetryPrefix",
            code,
            6
        );
    }

    private static byte[] TokenFixture(bool shared = false, bool alternateEntry = false, bool differentField = false)
    {
        const int longer = 3;
        const int retry = 4;
        const int fallback = 5;
        byte[] extraArm = alternateEntry ? [A(4), F(retry)] : [];
        byte[] secondFailure = shared ? [F(fallback)] : [F(retry)];
        byte[] longPrefix = shared
            ? [.. Cell(
                5,
                1,
                2,
                2
            ), .. Field(3), .. Cell(
                5,
                1,
                2,
                2
            ), .. Field(4)]
            : [
                .. Cell(
                    retry,
                    1,
                    2,
                    2
                ),
                IsNonemptyList, F(retry), X(2), .. Field(3),
                GetList, X(2), X(1), X(2), IsTaggedTuple, F(retry), X(1), U(2), A(4),
                IsNonemptyList, .. secondFailure, X(2), .. Field(4)
            ];
        byte[] extension = shared
            ? [
                IsNonemptyList, F(retry), X(2), GetList, X(2), X(6), X(7),
                IsTaggedTuple, F(retry), X(6), U(3), A(4), GetTupleElement, X(6), U(1), X(5),
                Move, X(6), X(1), Move, X(7), X(2)
            ]
            : [
                GetList, X(2), X(1), X(2), IsTaggedTuple, F(retry), X(1), U(3), A(4), .. Field(5)
            ];
        byte[] shortPrefix = shared
            ? []
            : [
                GetList, X(0), X(1), X(2), .. Cell(
                    fallback,
                    1,
                    2,
                    2
                ), .. Field(3),
                .. Cell(
                    fallback,
                    1,
                    2,
                    2
                ), GetTupleElement, X(1), U(differentField ? 0 : 1), X(4)
            ];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            IsNonemptyList, F(fallback), X(0), GetList, X(0), X(1), X(2),
            IsTuple, F(fallback), X(1), TestArity, F(fallback), X(1), U(2), GetTupleElement, X(1), U(0), X(1),
            SelectValue, X(1), F(fallback), ExtendedListOperand, U(alternateEntry ? 4 : 2), A(3), F(longer), .. extraArm,
            Label, U(longer), .. longPrefix, .. extension, Allocate, U(0), U(6), Move, X(2), X(0), Deallocate, U(0), Return,
            Label, U(retry), .. shortPrefix, Allocate, U(0), U(5), Move, X(2), X(0), Deallocate, U(0), Return,
            Label, U(fallback), Return, EndCode
        ];

        static byte[] Cell(
            int failure,
            int head,
            int tail,
            int arity
        ) =>
        [
            IsNonemptyList, F(failure), X(tail), GetList, X(tail), X(head), X(tail),
            IsTaggedTuple, F(failure), X(head), U(arity), A(4)
        ];

        static byte[] Field(int output) => [GetTupleElement, X(1), U(1), X(output)];
    }

    private static byte[] ArityFixture(
        bool selected = false,
        bool otherEntry = false,
        bool liveFallback = false,
        bool otherRetryTag = false,
        bool delimiter = false,
        bool unequalSharedTag = false
    )
    {
        const int other = 3;
        const int retry = 4;
        const int fallback = 5;
        const int first = 6;
        const int completed = 7;
        byte[] init = liveFallback ? [Move, A(5), X(2)] : [];
        byte[] alternatePredecessor = otherEntry ? [IsAtom, F(other), X(0)] : [];
        byte[] finish = liveFallback ? [Move, X(2), X(0), Return] : [Return];
        byte[] shared = delimiter ? TailGuard(fallback, 3) : [];
        byte[] continuation = delimiter ? TailGuard(fallback, 5) : [];
        byte[] longerSetup = delimiter && !unequalSharedTag
            ?
            [
                .. shared, IsNonemptyList, F(fallback), X(2), GetList, X(2), X(1), X(2),
                IsTuple, F(fallback), X(1), TestArity, F(fallback), X(1), U(2), GetTupleElement, X(1), U(0), X(3),
                SelectValue, X(3), F(fallback), ExtendedListOperand, U(4), A(4), F(retry), A(3), F(completed),
                Label, U(completed)
            ]
            : [.. (delimiter ? TailGuard(retry, 3) : []), .. TailGuard(retry, 3)];
        byte[] retrySetup = delimiter && !unequalSharedTag
            ? []
            : [GetList, X(0), X(1), X(2), .. (delimiter ? TailGuard(fallback, 5) : []), .. TailGuard(fallback, 4)];

        return selected
            ?
            [
                .. Start(
                    1,
                    2,
                    2,
                    1
                ),
                IsNonemptyList, F(fallback), X(0), GetList, X(0), X(1), X(2), IsTuple, F(fallback), X(1),
                SelectTupleArity, ExtendedTypedRegisterOperand, X(1), U(1), F(fallback), ExtendedListOperand, U(4),
                U(2), F(other), U(3), F(first),
                Label, U(first), GetTupleElement, X(1), U(0), X(3), ExactEqualTest, F(fallback), X(3), A(3),
                .. longerSetup, .. Result(3),
                Label, U(retry), .. retrySetup, .. continuation, .. Result(4),
                Label, U(other), GetTupleElement, X(1), U(0), X(1), ExactEqualTest, F(fallback), X(1), A(4),
                .. TailGuard(fallback, 5), .. Result(5),
                Label, U(fallback), Return, EndCode
            ]
            :
            [
                .. Start(
                    1,
                    2,
                    2,
                    1
                ),
                .. init, .. alternatePredecessor,
                .. Split(
                    other,
                    3,
                    3
                ),
                .. (delimiter ? TailGuard(other, 3) : []), .. TailGuard(other, 3), .. Result(3),
                Label, U(other), .. Split(
                    retry,
                    2,
                    4
                ),
                .. TailGuard(retry, 5), .. Result(5),
                Label, U(retry), .. Split(
                    fallback,
                    3,
                    otherRetryTag ? 4 : 3
                ),
                .. (delimiter ? TailGuard(fallback, unequalSharedTag ? 5 : 3) : []),
                .. TailGuard(fallback, 4), .. continuation, .. Result(4),
                Label, U(fallback), .. finish, EndCode
            ];

        static byte[] Split(
            int failure,
            int arity,
            int tag
        ) =>
        [
            IsNonemptyList, F(failure), X(0), GetList, X(0), X(1), X(2),
            IsTaggedTuple, F(failure), X(1), U(arity), A(tag)
        ];

        static byte[] TailGuard(int failure, int tag) =>
        [
            IsNonemptyList, F(failure), X(2), GetList, X(2), X(1), X(2),
            IsTaggedTuple, F(failure), X(1), U(2), A(tag)
        ];

        static byte[] Result(int value) =>
        [
            Allocate, U(0), U(0), Move, A(value), X(0), Deallocate, U(0), Return
        ];
    }

    private static int Operand(object instruction, int index) =>
        (int)(BigInteger)Property(Read(instruction, "Operands").ElementAt(index), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Read(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>();
}
