using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedBinaryTailCaseChecks
{
    internal const string TestName = "compiler/beam/skips-guarded-binary-prefix-without-a-frame";
    private const string WrongLayout = "A checked binary suffix needs only a match start, skip and tail extraction.";
    private const string WrongValue = "The guarded suffix or its fallback returned the wrong value.";
    private const string Source = """
        -module(beam_guarded_binary_tail_case).
        -export([drop/2, with_list/2, retains_input/2, bytes/0, unaligned/0, tail/0, empty/0]).

        drop(Bin, Count) when is_binary(Bin) ->
            case Bin of
                <<_:Count/binary, Tail/binary>> -> Tail;
                _ -> <<>>
            end.

        with_list(Count, Bin) when erlang:is_binary(Bin) ->
            case Bin of
                <<_:Count/binary, Tail/binary>> -> Tail;
                _ -> <<>>
            end;
        with_list(Count, List) when is_list(List) ->
            report(Count, List).

        retains_input(Bin, Count) when is_binary(Bin) ->
            case Bin of
                <<_:Count/binary, Tail/binary>> -> Tail;
                _ -> Bin
            end.

        report(Count, List) -> {Count, List}.
        bytes() -> <<1, 2, 3>>.
        unaligned() -> <<1:1>>.
        tail() -> <<2, 3>>.
        empty() -> <<>>.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_guarded_binary_tail_case.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (entries.Length < 4)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        for (int index = 0; index < 2; index++)
        {
            byte[] body = opcodes[(entries[index] + 2)..entries[index + 1]];
            if (body is not [IsBinary, StartBinaryMatchKnown, SkipBinaryBits, GetBinaryTail, Return, ..]
                || body.Any(opcode => opcode is Allocate or AllocateHeap or GetBinarySegment))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        var module = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = await module.Invoke(context, "bytes");
            Term suffix = await module.Invoke(context, "tail");
            Term empty = await module.Invoke(context, "empty");
            Term one = Term.I(1);
            Term list = Term.List(Term.A("item"));
            if (!suffix.Equals(await module.Invoke(
                context,
                "drop",
                input,
                one
            ))
                || !input.Equals(await module.Invoke(
                    context,
                    "drop",
                    input,
                    Term.I(0)
                ))
                || !empty.Equals(await module.Invoke(
                    context,
                    "drop",
                    input,
                    Term.I(3)
                ))
                || !suffix.Equals(await module.Invoke(
                    context,
                    "with_list",
                    one,
                    input
                ))
                || !Term.Tuple(one, list).Equals(await module.Invoke(
                    context,
                    "with_list",
                    one,
                    list
                ))
                || !input.Equals(await module.Invoke(
                    context,
                    "retains_input",
                    input,
                    Term.I(4)
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            foreach (Term count in new Term[] { Term.I(-1), Term.I(4), Term.I(long.MaxValue), Term.A("length"), new FloatTerm(1.0) })
            {
                if (!empty.Equals(await module.Invoke(
                    context,
                    "drop",
                    input,
                    count
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            Term unaligned = await module.Invoke(context, "unaligned");
            try
            {
                await module.Invoke(
                    context,
                    "drop",
                    unaligned,
                    one
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }
}
