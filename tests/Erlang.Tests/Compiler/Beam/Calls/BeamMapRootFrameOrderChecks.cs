using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Tests;

internal static class BeamMapRootFrameOrderChecks
{
    internal const string TestName = "compiler/beam/map-root-preserves-earlier-frame-compaction";
    private const string ExpectedFrameAndRoots = "Expected both throw maps to keep their live X0 root without adding a frame slot.";

    internal static Task Compare()
    {
        const string source = """
            -module(map_frame_probe).
            -export([parse/1]).
            parse(Text) ->
                Chars = lists:flatten(unicode:characters_to_list(Text)),
                maybe
                    {ok, Tokens, _} ?= erl_scan:string(Chars, {1,1}, [text]),
                    Ready = prepare(Tokens),
                    {ok, Exprs} ?= identity(erl_eval:extended_parse_exprs(Ready)),
                    Exprs
                else
                    {error, {{Line, _}, _, _} = Error, _} ->
                        [{_, Text1}] = sys_messages:format_messages("", "", [Error], []),
                        throw({fault, #{line => Line, message => string:trim(Text1, leading, ":")}});
                    {error, {{Line, _}, _, _} = Error} ->
                        [{_, Text1}] = sys_messages:format_messages("", "", [Error], []),
                        throw({fault, #{line => Line, message => string:trim(Text1, leading, ":")}})
                end.
            prepare([{atom, Pos, alias_name} | Tail]) -> [{atom, Pos, rewritten} | prepare(Tail)];
            prepare([Token | Tail]) -> [Token | prepare(Tail)];
            prepare([]) -> [].
            identity(Thing) -> Thing.
            """;
        byte[] beam = CodeGeneration.CompileBeamModule(source, "map_frame_probe.erl");
        ReadOnlySpan<byte> code = BeamDocumentationEmissionChecks.Chunks(beam).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> entry = [Label, 0x20, Allocate];
        int frame = code.IndexOf(entry);
        if (frame < 0 || code[frame + entry.Length] is not (0x10 or 0x20 or 0x30 or 0x40)
            || CountMapRoots(code) != 2)
        {
            throw new InvalidOperationException(ExpectedFrameAndRoots);
        }

        return Task.CompletedTask;
    }

    private static int CountMapRoots(ReadOnlySpan<byte> code)
    {
        ReadOnlySpan<byte> map = [PutMapAssoc, 0x05, ExtendedLiteralOperand, 0x30, 0x03];
        int count = 0;
        for (int index = 0; index <= code.Length - 14; index++)
        {
            if (code[index..].StartsWith(map) && code[index + 5] is 0x10 or 0x20 && code[index + 13] == 0x03)
            {
                count++;
            }
        }

        return count;
    }
}
