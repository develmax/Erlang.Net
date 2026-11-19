using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamPreCallTransientSlotChecks
{
    internal const string TestName = "compiler/beam/promotes-only-an-expired-pre-call-frame-slot";
    private const string ExpectedFrame = "Expected only values needed after the call to occupy Y slots.";

    internal static Task Compare()
    {
        const string beforeCall = """
            -module(pre_call_transient_slot).
            -export([entry/1]).
            entry({Group, Prefix, Model, Level, Row}) ->
                Key = make_key(Group, Prefix, Model, Level),
                {Key, Row}.
            make_key(Group, Prefix, Model, Level) -> erlang:garbage_collect(), {erlang:phash2(Group), Prefix, Model, Level}.
            """;
        const string acrossCall = """
            -module(pre_call_transient_slot).
            -export([entry/1]).
            entry({Group, Prefix, Model, Level, Row}) ->
                Key = make_key(Group, Prefix, Model, Level),
                {Key, Group, Row}.
            make_key(Group, Prefix, Model, Level) -> erlang:garbage_collect(), {erlang:phash2(Group), Prefix, Model, Level}.
            """;

        if (ReadEntryFrame(beforeCall) != 1 || ReadEntryFrame(acrossCall) < 2)
        {
            throw new InvalidOperationException(ExpectedFrame);
        }

        return Task.CompletedTask;
    }

    private static int ReadEntryFrame(string source)
    {
        byte[] beam = CodeGeneration.CompileBeamModule(source, "pre_call_transient_slot.erl");
        _ = BeamModule.Prepare(beam);
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> entry = [IsTuple, 0x15, 0x03, TestArity, 0x15, 0x03, 0x50, Allocate];
        int index = code.IndexOf(entry);
        if (index < 0 || index + entry.Length + 1 >= code.Length)
        {
            throw new InvalidOperationException(ExpectedFrame);
        }

        byte slots = code[index + entry.Length];
        byte live = code[index + entry.Length + 1];
        if ((slots & 0x0f) != 0 || live != 0x10)
        {
            throw new InvalidOperationException(ExpectedFrame);
        }

        return slots >> 4;
    }
}
