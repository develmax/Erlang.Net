using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRawPositionedPayloadChecks
{
    internal const string TestName = "compiler/beam/keeps-raw-payload-cursor-through-integer-width-arithmetic";
    private const string WrongLayout = "Expected the prefix and its computed payload to share one frameless cursor.";
    private const string WrongValue = "Unexpected payload or failure from the positioned reader.";
    private const string Source = """
        -module(raw_positioned_payload).
        -export([parse/1, exported_decode/1, unknown/1]).

        parse(Binary) -> decode(<<Binary/binary, 255>>).
        decode(<<Total:16, Pad:8, Suffix/binary>>) ->
            Width = Total - Pad - 1,
            <<Payload:Width/binary, _/binary>> = Suffix,
            Payload.

        exported_decode(<<Total:16, Pad:8, Suffix/binary>>) ->
            Width = Total - Pad - 1,
            <<Payload:Width/binary, _/binary>> = Suffix,
            Payload.

        unknown(Value) -> maybe_bytes(Value).
        maybe_bytes(<<Total:16, Pad:8, Suffix/binary>>) ->
            Width = Total - Pad - 1,
            <<Payload:Width/binary, _/binary>> = Suffix,
            Payload.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "raw_positioned_payload.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (entries.Length != 7)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] proven = opcodes[entries[1]..entries[2]];
        byte[] exported = opcodes[entries[2]..entries[3]];
        byte[] unknown = opcodes[entries[4]..entries[5]];
        int start = Array.IndexOf(proven, StartBinaryMatchKnown);
        int prefix = Array.IndexOf(proven, BinaryMatch);
        int payload = Array.IndexOf(proven, GetBinarySegment);
        if (start < 0 || proven[start + 1] != GetBinaryPosition || prefix != start + 2 || payload <= prefix
            || proven[prefix..payload].Count(opcode => opcode == GcBifTwo) != 2
            || proven[payload..].Count(opcode => opcode == GetBinaryTail) != 2
            || !proven[payload..].Contains(SetBinaryPosition) || proven.Contains(Allocate)
            || !exported.Contains(StartBinaryMatch) || !unknown.Contains(StartBinaryMatch))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var image = BeamModule.Prepare(bytes, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!new BitString([65, 66]).Equals(await image.Invoke(context, "parse", new BitString([0, 4, 1, 65, 66])))
                || !new BitString([]).Equals(await image.Invoke(context, "parse", new BitString([0, 1, 0]))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term badPayload = Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), new BitString([65, 255]));
            await ExpectFailure("parse", new BitString([0, 0, 2, 65]), badPayload);
            await ExpectFailure("parse", new BitString([0, 8, 0, 65]), badPayload);
            await ExpectFailure("parse", new BitString([1]), Term.A(ErlangErrorReasons.FunctionClause));
            await ExpectFailure("unknown", Term.A("not_binary"), Term.A(ErlangErrorReasons.FunctionClause));

            return Nil.Value;

            async Task ExpectFailure(string function, Term input, Term reason)
            {
                try
                {
                    await image.Invoke(context, function, input);
                    throw new InvalidOperationException(WrongValue);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(reason))
                {
                }
            }
        });
    }
}
