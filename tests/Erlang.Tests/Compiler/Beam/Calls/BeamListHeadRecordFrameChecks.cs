using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListHeadRecordFrameChecks
{
    internal const string TestName = "compiler/beam/checks-list-head-record-before-owned-frame";

    private const string Source = """
        -module(list_head_record_frame_fixture).
        -export([nil_fallback/2, owned_fallback/2, unchecked/2, disjunction/1]).
        -record(item, {value}).

        nil_fallback([Head|Tail], Effect) when is_record(Head, item) ->
            Result = list_head_record_frame_provider:note(Effect),
            {Head,Tail,Result};
        nil_fallback([], _) ->
            empty.

        owned_fallback([Head|Tail], Effect) when erlang:is_record(Head, item, 2) ->
            Result = list_head_record_frame_provider:note(Effect),
            {Head,Tail,Result};
        owned_fallback(Original, Effect) ->
            Result = list_head_record_frame_provider:note({fallback,Effect}),
            {Original,Result}.

        unchecked([Head|Tail], Effect) ->
            Result = list_head_record_frame_provider:note(Effect),
            {Head#item.value,Tail,Result};
        unchecked([], _) ->
            empty.

        disjunction([Head|Tail]) when is_record(Head, item) orelse Head =:= skip ->
            Result = list_head_record_frame_provider:note(Head),
            {Tail,Result};
        disjunction(Original) ->
            {fallback,Original}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "list_head_record_frame_fixture.erl"));
        VerifyCheckedArm(code, "nil_fallback", framedFallback: false);
        VerifyCheckedArm(code, "owned_fallback", framedFallback: true);

        byte[] uncheckedAccess = ReadOpcodes(code, "unchecked");
        int effect = Array.IndexOf(uncheckedAccess, ExternalCall);
        int bodyCheck = Array.IndexOf(uncheckedAccess, IsTaggedTuple);
        if (effect < 0 || effect >= bodyCheck || Array.IndexOf(uncheckedAccess, BadRecord) <= bodyCheck)
        {
            throw new InvalidOperationException(BeamListHeadRecordFrameDiagnostics.BodyFailure);
        }
    }

    private static void VerifyCheckedArm(object code, string function, bool framedFallback)
    {
        byte[] operations = ReadOpcodes(code, function);
        int uncons = Array.IndexOf(operations, GetList);
        int check = Array.IndexOf(operations, IsTaggedTuple);
        int frame = Array.FindIndex(operations, IsFrame);
        int call = Array.IndexOf(operations, ExternalCall);
        if (uncons < 0 || uncons >= check || check >= frame || frame >= call)
        {
            throw new InvalidOperationException(BeamListHeadRecordFrameDiagnostics.EntryOwnership);
        }

        int fallbackFrame = Array.FindIndex(operations, call + 1, IsFrame);
        int fallbackCall = Array.IndexOf(operations, ExternalCall, call + 1);
        if (framedFallback ? fallbackFrame < 0 || fallbackFrame >= fallbackCall
            : fallbackFrame >= 0 || fallbackCall >= 0)
        {
            throw new InvalidOperationException(BeamListHeadRecordFrameDiagnostics.FallbackOwnership);
        }
    }

    private static bool IsFrame(byte opcode) => opcode is Allocate or AllocateHeap;

    private static byte[] ReadOpcodes(object code, string function)
    {
        object entry = Items(code, "Functions").Single(item => (string)Property(item, "Name") == function);
        int start = (int)Property(entry, "FirstInstructionIndex");
        int end = (int)Property(entry, "EndInstructionIndex");

        return Items(code, "Instructions")[start..end].Select(Opcode).ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
