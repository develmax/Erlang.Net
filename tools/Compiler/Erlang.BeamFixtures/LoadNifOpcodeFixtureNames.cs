namespace BeamFixtureExport;

internal static class LoadNifOpcodeFixtureNames
{
    internal const string Directory = "load-nif-opcodes";
    internal const string Module = "beam_fixture_load_nif_opcodes";
    internal const string Manifest = "load-nif-opcode-manifest.json";
    internal const string Predeclared = "predeclared-contract.json";
    internal const string FunctionsEtf = "functions.etf";
    internal const string Disassembly = "disassembly.term";
    internal const string DisassemblyEvaluation = "disassembly.eval.erl";
    internal const string CompilerPrefix = "compile";
    internal const string DisassemblyPrefix = "disassemble";
    internal const string NonTailRole = "non_tail/3: call_ext erlang:load_nif/2";
    internal const string LastRole = "tail_last/3: call_ext_last erlang:load_nif/2 with positive retained Y frame";
    internal const string OnlyRole = "tail_only/2: call_ext_only erlang:load_nif/2";
    internal const string Purpose = "Original compilation and file-only disassembly of three literal load_nif caller roles; original body/native loading execution is not requested";
    internal const string ShapeStatus = "Unverified hypothesis until the independent verifier accepts actual original disassembly";
    internal const string DisassemblyExpressionPrefix = "D=beam_disasm:file(";
    internal const string DisassemblyExpressionMiddle = "),{beam_file,beam_fixture_load_nif_opcodes,_,_,_,Functions}=D,ok=file:write_file(";
    internal const string DisassemblyExpressionEtf = ",term_to_binary({load_nif_opcode_functions,beam_fixture_load_nif_opcodes,Functions})),ok=file:write_file(";
    internal const string DisassemblyExpressionEnd = ",unicode:characters_to_binary(io_lib:format(\"~tp.~n\",[D]))),halt(0).";
    internal const string Quote = "\"";
    internal const string Backslash = "\\";
    internal const string EscapedBackslash = "\\\\";
    internal const string EscapedQuote = "\\\"";
    internal const string CarriageReturn = "\r";
    internal const string LineFeed = "\n";
    internal const string EscapedCarriageReturn = "\\r";
    internal const string EscapedLineFeed = "\\n";
    internal const string Source = """
        -module(beam_fixture_load_nif_opcodes).
        -export([non_tail/3,tail_last/3,tail_only/2]).

        non_tail(Path,Info,Marker) ->
            Result=erlang:load_nif(Path,Info),
            {Marker,Result}.

        tail_last(Path,Info,Marker) ->
            erlang:put(nif_frame_marker,Marker),
            erlang:load_nif(Path,Info).

        tail_only(Path,Info) ->
            erlang:load_nif(Path,Info).
        """;
}
