using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamTupleEntryListCallChecks
{
    private const string MissingRegisterProjection = "The tuple fields and the nullary BIF result must be constructed in X registers.";
    private const string InvalidFrameRemoval = "A call or a retained tuple field requires preserving the frame.";

    internal static void Check()
    {
        const string positive = """
            -module(beam_tuple_entry_list_call).
            -export([run/1, permuted/1]).
            run({A,B,C}) ->
                List = [A,B,C,self()],
                Result = dep:observe(first,second,List),
                {ok,Result}.
            permuted({A,B}) ->
                List = [B,node(),A],
                Result = dep:observe(List,first,second),
                {ready,Result}.
            """;
        const string effectful = """
            -module(beam_tuple_entry_list_effect).
            -export([run/1]).
            -compile({no_auto_import,[self/0]}).
            run({A,B,C}) ->
                List = [A,B,C,self()],
                Result = dep:observe(first,second,List),
                {ok,Result}.
            self() -> dep:effect().
            """;
        const string retained = """
            -module(beam_tuple_entry_list_retained).
            -export([run/1]).
            run({A,B,C}) ->
                List = [A,B,C,self()],
                Result = dep:observe(first,second,List),
                {A,Result}.
            """;

        ReadOnlySpan<byte> wide =
        [
            AllocateHeap, 0, 0x80, 0x10,
            GetTupleElement, 0x03, 0, 0x13,
            GetTupleElement, 0x03, 0x10, 0x23,
            GetTupleElement, 0x03, 0x20, 0x03,
            BifZero
        ];
        ReadOnlySpan<byte> narrow =
        [
            AllocateHeap, 0, 0x60, 0x10,
            GetTupleElement, 0x03, 0, 0x13,
            GetTupleElement, 0x03, 0x10, 0x03,
            BifZero
        ];
        ReadOnlySpan<byte> wideCons =
        [
            PutList, 0x33, 0x02, 0x33,
            PutList, 0x03, 0x33, 0x03,
            PutList, 0x23, 0x03, 0x03,
            PutList, 0x13, 0x03, 0x23
        ];
        byte[] code = CompileCode(positive, "beam_tuple_entry_list_call.erl");
        if (code.AsSpan().IndexOf(wide) < 0 || code.AsSpan().IndexOf(narrow) < 0 || code.AsSpan().IndexOf(wideCons) < 0)
        {
            throw new InvalidOperationException(MissingRegisterProjection);
        }

        if (CompileCode(effectful, "beam_tuple_entry_list_effect.erl").AsSpan().IndexOf(wide) >= 0
            || CompileCode(retained, "beam_tuple_entry_list_retained.erl").AsSpan().IndexOf(wide) >= 0)
        {
            throw new InvalidOperationException(InvalidFrameRemoval);
        }
    }

    private static byte[] CompileCode(string source, string file)
    {
        byte[] module = CodeGeneration.CompileBeamModule(source, file);
        _ = BeamModule.Prepare(module);

        return BeamDocumentationEmissionChecks.Chunks(module).Single(chunk => chunk.Id == "Code").Data;
    }
}
