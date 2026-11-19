using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamSiblingConstructorChecks
{
    internal const string TestName = "compiler/beam/sibling-constructors-preserve-core-order";
    private const string ExpectedConstructorOrder = "Expected Core's constructor order, live registers and ordinary dynamic append.";

    internal static Task Compare()
    {
        const string source = """
            -module(sibling_constructor_probe).
            -export([direct/1, append/3, opaque/3]).
            direct(Name) -> {Name,{Name,pick,[]},[Name,done]}.
            append(Name, Args, Tail) -> {Name,{Name,pick,Args},[Name] ++ Tail}.
            opaque(Name, Prefix, Tail) -> {Name,{Name,pick,[]},Prefix ++ Tail}.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "sibling_constructor_probe.erl");
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> direct =
        [
            TestHeap, 0xa0, 0x10,
            PutTuple, 0x13, ExtendedListOperand, 0x30, 0x03, 0x32, 0x02,
            PutList, 0x03, ExtendedLiteralOperand, 0x00, 0x23
        ];
        ReadOnlySpan<byte> append =
        [
            TestHeap, 0xa0, 0x30,
            PutList, 0x03, 0x23, 0x23,
            PutTuple, 0x13, ExtendedListOperand, 0x30, 0x03, 0x32, 0x13,
            PutTuple, 0x03, ExtendedListOperand, 0x30, 0x03, 0x13, 0x23,
            Return
        ];
        ReadOnlySpan<byte> dynamicAppend = [ExternalCall, 0x20, 0x00];
        if (code.IndexOf(direct) < 0 || code.IndexOf(append) < 0 || code.IndexOf(dynamicAppend) < 0)
        {
            throw new InvalidOperationException(ExpectedConstructorOrder);
        }

        return Task.CompletedTask;
    }
}
