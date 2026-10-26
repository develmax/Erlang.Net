namespace Erlang.Otp;

internal static class ProcLibReportPrettyNames
{
    public const string Ellipsis = "...";
    public const string EmptyList = "[]";
    public const string EmptyTuple = "{}";
    public const string EmptyMap = "#{}";
    public const string EmptyBinary = "<<>>";
    public const string TupleOpen = "{";
    public const string TupleClose = "}";
    public const string MapOpen = "#{";
    public const string BinaryOpen = "<<";
    public const string BinaryClose = ">>";
    public const string RecordPrefix = "#";
    public const string RecordModuleSeparator = ":";
    public const string BitSizeSeparator = ":";
    public const string LineAssociation = " =>\n";
    public const string LineFieldAssignment = " =";
    public const string Association = " => ";
    public const string FieldSeparator = ",";
    public const string FieldAssignment = " = ";
    public const string Utf8Suffix = "/utf8";
    public const string ShortFloat = "short";
    public const string Unsupported = "The report pretty printer does not yet implement this OTP layout branch";
}
