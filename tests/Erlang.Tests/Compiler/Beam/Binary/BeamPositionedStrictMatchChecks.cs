using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedStrictMatchChecks
{
    internal const string TestName = "compiler/beam/keeps-original-suffix-for-positioned-strict-match-failure";
    private const string CodeMismatch = "Expected one binary match context with rollback on the strict failure.";
    private const string ValueMismatch = "Unexpected positioned match result or failure payload.";
    private const string Source = """
        -module(positioned_strict_match).
        -export([strict/1, integer_field/1, escaping/1, failure/1, sibling_scope/2, exported_scope/2]).

        strict(Input) ->
            <<Count:3, Rest/bitstring>> = Input,
            Width = Count + 1,
            Pad = erlang:bit_size(Rest) band 7,
            <<_:Pad, Payload:Width/binary, Tail/bitstring>> = Rest,
            {Payload, Tail}.

        integer_field(Input) ->
            <<Count:5, Rest/bitstring>> = Input,
            Width = Count + 2,
            Pad = erlang:bit_size(Rest) band 3,
            <<_:Pad, Payload:Width/unsigned-unit:8, Tail/bitstring>> = Rest,
            {Payload, Tail}.

        escaping(Input) ->
            <<Count:3, Rest/bitstring>> = Input,
            Width = Count + 1,
            Pad = erlang:bit_size(Rest) band 7,
            <<_:Pad, Payload:Width/binary, Tail/bitstring>> = Rest,
            {Rest, Payload, Tail}.

        failure(Input) ->
            try strict(Input) of
                Result -> {success, Result}
            catch
                error:{badmatch, Payload} -> {badmatch, Payload}
            end.
        sibling_scope(Kind, Input) ->
            case Kind of
                decode ->
                    <<Count:3, Rest/bitstring>> = Input,
                    Width = Count + 1,
                    Pad = erlang:bit_size(Rest) band 7,
                    <<_:Pad, Payload:Width/binary, Tail/bitstring>> = Rest,
                    {Payload, Tail};
                independent ->
                    Rest = {sibling, Input},
                    Rest
            end.

        exported_scope(Kind, Input) ->
            Decoded = case Kind of
                decode ->
                    <<Count:3, Rest/bitstring>> = Input,
                    Width = Count + 1,
                    Pad = erlang:bit_size(Rest) band 7,
                    <<_:Pad, Payload:Width/binary, Tail/bitstring>> = Rest,
                    {Payload, Tail};
                independent ->
                    Rest = {sibling, Input},
                    independent
            end,
            {Decoded, Rest}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "positioned_strict_match.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] starts = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (starts.Length < 7)
        {
            throw new InvalidOperationException(CodeMismatch);
        }

        for (int index = 0; index < 2; index++)
        {
            byte[] body = opcodes[starts[index]..starts[index + 1]];
            int restore = Array.IndexOf(body, SetBinaryPosition);
            if (body.Count(opcode => opcode is StartBinaryMatch or StartBinaryMatchKnown) != 1
                || !body.Contains(GetBinaryPosition) || !body.Contains(SkipBinaryBits) || restore < 0
                || !body.Skip(restore + 1).Contains(GetBinaryTail))
            {
                throw new InvalidOperationException(CodeMismatch);
            }
        }

        if (opcodes[starts[2]..starts[3]].Contains(SetBinaryPosition)
            || !opcodes[starts[4]..starts[5]].Contains(SetBinaryPosition)
            || opcodes[starts[5]..starts[6]].Contains(SetBinaryPosition))
        {
            throw new InvalidOperationException(CodeMismatch);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = new BitString([22, 175, 52], 22);
            Term payload = new BitString([171]);
            Term tail = new BitString([205]);
            Term suffix = new BitString([181, 121, 160], 19);
            Term empty = new BitString([]);
            Term shortPrefix = new BitString([0], 2);
            Term shortPayload = new BitString([86, 175, 52], 22);
            Term integerInput = new BitString([5, 87, 154], 23);
            if (!Term.Tuple(payload, tail).Equals(await image.Invoke(context, "strict", input))
                || !Term.Tuple(Term.I(43981), empty).Equals(await image.Invoke(context, "integer_field", integerInput))
                || !Term.Tuple(suffix, payload, tail).Equals(await image.Invoke(context, "escaping", input))
                || !Term.Tuple(payload, tail).Equals(
                    await image.Invoke(
                        context,
                        "sibling_scope",
                        Term.A("decode"),
                        input
                    ))
                || !Term.Tuple(Term.A("sibling"), input).Equals(
                    await image.Invoke(
                        context,
                        "sibling_scope",
                        Term.A("independent"),
                        input
                    ))
                || !Term.Tuple(Term.Tuple(payload, tail), suffix).Equals(
                    await image.Invoke(
                        context,
                        "exported_scope",
                        Term.A("decode"),
                        input
                    ))
                || !Term.Tuple(Term.A("badmatch"), suffix).Equals(await image.Invoke(context, "failure", shortPayload))
                || !Term.Tuple(Term.A("badmatch"), shortPrefix).Equals(await image.Invoke(context, "failure", shortPrefix)))
            {
                throw new InvalidOperationException(ValueMismatch);
            }

            return Nil.Value;
        });
    }
}
