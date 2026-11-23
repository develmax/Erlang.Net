using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonEntryFramesChecks
{
    internal const string TestName = "compiler/beam/places-singleton-check-before-frame";
    internal const string Fixture = "beam_singleton_entry_frame.erl";
    private const string FrameBeforeCheck = "Expected the singleton check before the one-slot success frame.";
    private const string LostTail = "The nonempty-list clause did not retain the variable tail.";

    internal const string Source = """
        -module(beam_singleton_entry_frame).
        -export([plain/1, keeps_tail/1, with_fallback/1, observe/0]).

        plain([Head]) ->
            checkpoint(),
            {ok, Head}.

        keeps_tail([Head | Tail]) ->
            checkpoint(),
            {Head, Tail}.

        with_fallback([Head]) ->
            checkpoint(),
            {single, Head};
        with_fallback(Other) ->
            {other, Other}.

        checkpoint() ->
            NewCount = case get(count) of
                undefined -> 1;
                Count -> Count + 1
            end,
            put(count, NewCount),
            ok.

        observe() ->
            erase(count),
            Plain = plain([example]),
            Tail = keeps_tail([head | improper]),
            Fallback = with_fallback([first, second]),
            Error = try plain([one | invalid])
                    catch error:function_clause:Trace ->
                        [{?MODULE, plain, Args, Location} | _] = Trace,
                        {Args, proplists:get_value(line, Location)}
                    end,
            {Plain, Tail, Fallback, Error, get(count)}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(Opcode).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();

        byte[] single = opcodes[entries[0]..entries[1]];
        var check = Array.IndexOf(single, IsNil);
        var frame = Array.IndexOf(single, Allocate);
        if (check < 0 || frame < check || !single[..check].Contains(GetList))
        {
            throw new InvalidOperationException(FrameBeforeCheck);
        }

        object frameInstruction = instructions[entries[0] + frame];
        object width = ((IEnumerable)Property(frameInstruction, "Operands")).Cast<object>().First();
        if (Property(width, "Value").ToString() != "1")
        {
            throw new InvalidOperationException(FrameBeforeCheck);
        }

        byte[] variableTail = opcodes[entries[1]..entries[2]];
        if (!variableTail.Contains(GetList) || variableTail.Contains(IsNil))
        {
            throw new InvalidOperationException(LostTail);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
