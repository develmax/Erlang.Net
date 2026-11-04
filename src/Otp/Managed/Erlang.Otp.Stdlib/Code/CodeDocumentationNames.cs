namespace Erlang;

internal static class CodeDocumentationNames
{
    internal const string Missing = "missing";
    internal const string Eep48 = "eep48";
    internal const string DebugInfo = "debug_info";
    internal const string Sources = "sources";
    internal const string Function = "function";
    internal const string Attribute = "attribute";
    internal const string Type = "type";
    internal const string Spec = "spec";
    internal const string Signature = "signature";
    internal const string None = "none";
    internal const string AbstractCode = "abstract_code";
    internal const string RawAbstract = "raw_abstract_v1";
    internal const string DocsChunk = "Docs";
    internal const string MissingChunk = "missing_chunk";
    internal const string FileError = "file_error";
    internal const string NoAbstractCode = "no_abstract_code";
    internal const string RecordTag = "docs_v1";
    internal const string Language = "erlang";
    internal const string Generated = "generated";
    internal const string OtpDocVersion = "otp_doc_vsn";
    internal const string SignatureFormat = "~p/~p";
    internal const string DocumentationDirectory = "doc";
    internal const string ChunkDirectory = "chunks";
    internal const string ChunkExtension = ".chunk";

    internal const int GeneratedAnnotation = 0;
    internal const int VersionMajor = 1;
    internal const int VersionMinor = 0;
    internal const int VersionPatch = 0;

    internal static ReadOnlySpan<byte> MimeType => "application/erlang+html"u8;
}
