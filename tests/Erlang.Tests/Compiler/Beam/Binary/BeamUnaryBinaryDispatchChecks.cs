using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamUnaryBinaryDispatchChecks
{
    internal const string TestName = "compiler/beam/groups-unary-binary-literal-clauses";
    private const string SourceFile = "beam_unary_binary_dispatch_fixture.erl";
    private const string IncorrectLayout = "The binary literal selectors, string matches or failure path differ.";
    private const string IncorrectValue = "The emitted literal dispatch selected the wrong clause.";

    private const string Source = """
        -module(beam_unary_binary_dispatch_fixture).
        -export([fixed/1, closed/1, short/1, prefix/1]).
        fixed(<<"shared-head-Xabcdefghijk">>) -> 1;
        fixed(<<"shared-head-Yabcdefghijk">>) -> 2;
        fixed(<<"shared-head-XBBCccccccc">>) -> 3;
        fixed(_) -> miss.
        closed(<<"shared-head-Xabcdefghijk">>) -> one;
        closed(<<"shared-head-Yabcdefghijk">>) -> two;
        closed(<<"shared-head-XBBCccccccc">>) -> three.
        short(<<"aaA12345678">>) -> a;
        short(<<"aaB1234567">>) -> b;
        short(<<"aaC123456">>) -> c;
        short(_) -> miss.
        prefix(<<"shared-head-X">>) -> exact;
        prefix(<<"shared-head-X", _/binary>>) -> open;
        prefix(_) -> miss.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, SourceFile);
        object[] instructions = ((IEnumerable)Property(Code(image), "Instructions")).Cast<object>().ToArray();
        byte[][] bodies = instructions.Select(Opcode).Aggregate(
            new List<List<byte>>(),
            (functions, opcode) =>
            {
                if (opcode == FunctionInfo)
                {
                    functions.Add([]);
                }
                else if (functions.Count > 0)
                {
                    functions[^1].Add(opcode);
                }

                return functions;
            }
        ).Select(body => body.ToArray()).ToArray();

        if (bodies[0].Count(opcode => opcode == MatchBinaryString) != 4
            || bodies[0].Count(opcode => opcode == SelectValue) != 1
            || !bodies[1].Contains(GetBinaryPosition)
            || !bodies[1].Contains(SetBinaryPosition)
            || !bodies[1].Contains(GetBinaryTail)
            || bodies[2].Contains(MatchBinaryString)
            || !bodies[2].Contains(SelectValue)
            || bodies[3].Contains(SelectValue))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            if (!Term.I(2).Equals(await module.Invoke(context, "fixed", new BitString("shared-head-Yabcdefghijk"u8.ToArray())))
                || !Term.A("miss").Equals(await module.Invoke(context, "fixed", new BitString("shared-head-Yabcdefghijk!"u8.ToArray())))
                || !Term.A("three").Equals(await module.Invoke(context, "closed", new BitString("shared-head-XBBCccccccc"u8.ToArray())))
                || !Term.A("c").Equals(await module.Invoke(context, "short", new BitString("aaC123456"u8.ToArray())))
                || !Term.A("exact").Equals(await module.Invoke(context, "prefix", new BitString("shared-head-X"u8.ToArray()))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
