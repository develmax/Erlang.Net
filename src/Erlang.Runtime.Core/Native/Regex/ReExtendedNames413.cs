namespace Erlang;

internal static class ReExtendedNames413
{
    internal const string Inspect = "inspect";
    internal const string Import = "import";
    internal const string InternalRun = "internal_run";
    internal const string NameList = "namelist";
    internal const string ExportedPattern = "re_exported_pattern";
    internal const string ExportOption = "export";
    internal const string SerializeDecode = "pcre2_serialize_decode_8";
    internal const string NativeMetadataFailure = "Original regex named capture metadata is inconsistent";
    internal const string UnsupportedSerializedPattern = "Serialized regex does not satisfy the admitted native code layout";
    internal const int NameNumberBytes = 2;
    internal const uint AllOptions = 0;
    internal const uint NameCount = 17;
    internal const uint NameEntrySize = 18;
    internal const uint NameTable = 19;
    internal const uint DuplicateNames = 0x00000040;
    internal const uint NoUtfCheck = 0x40000000;
    internal const int HeaderTitleBytes = 8;
    internal const int HeaderBytes = 14;
    internal const int ChecksumOffset = 8;
    internal const int EncodingOffset = 12;
    internal const int UnicodeOffset = 13;
    internal const byte EncodingVersion = 1;
    internal const uint CrcPolynomial = 0xEDB88320;
    internal const int BitsPerByte = 8;
    internal static ReadOnlySpan<byte> HeaderTitle => "re-PCRE2"u8;
}
