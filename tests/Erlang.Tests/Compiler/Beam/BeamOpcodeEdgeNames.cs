namespace Erlang.Tests;

internal static class BeamOpcodeEdgeNames
{
    internal const string AliasesTest = "runtime/beam/opcode-edges/get-list-aliases";
    internal const string TrimTest = "runtime/beam/opcode-edges/trim-zero-tail-continuation";
    internal const string BifTest = "runtime/beam/opcode-edges/guard-bif-destination";
    internal const string SelectTest = "runtime/beam/opcode-edges/select-table-admission";
    internal const string Resource = "guest_connected_bang322.beam";
    internal const string Module = "beam_opcode_edges373";
    internal const string Run = "run";
    internal const string Middle = "middle";
    internal const string Leaf = "leaf";
    internal const string Erlang = "erlang";
    internal const string Length = "length";
    internal const string Marker = "retained";
    internal const string Other = "returned";
    internal const string Atoms = "AtU8";
    internal const string Imports = "ImpT";
    internal const string Exports = "ExpT";
    internal const string Code = "Code";
    internal const string Failure = "BEAM opcode edge contract failed";
    internal static readonly string[] RemovedChunks = ["FunT", "LitT", "Type", "Attr", "Line", "LocT"];
}
