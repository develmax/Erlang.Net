namespace Erlang;

internal static class BeamDiagnostics
{
    public const string TruncatedField = "Truncated or negative-sized BEAM field";
    public const string SizePrefixDepth = "CLR compact size-prefix recursion limit exceeded";
    public const string ExtendedSizePrefix = "Invalid compact extended size prefix";
    public const string FormId = "Not a FOR1 form";
    public const string FormType = "Not a BEAM form";
    public const string ChunkSize = "Invalid chunk size";
    public const string ObsoleteAtoms = "Obsolete Atom table";
    public const string MissingAtoms = "Missing AtU8 table";
    public const string FormatVersion = "Unsupported BEAM format number";
    public const string AtomCount = "Invalid atom count";
    public const string LongAtomLength = "Invalid long atom length";
    public const string AtomLengthBounds = "Atom length exceeds input bounds";
    public const string Utf8Atom = "Invalid UTF8 atom";
    public const string ExportLabel = "Negative export label";
    public const string TableCount = "Invalid table count";
    public const string AtomIndex = "Atom index outside table";
    public const string FunctionArity = "Invalid function arity";

    public static string MissingChunk(string id) => "Missing " + id + " chunk";
}
