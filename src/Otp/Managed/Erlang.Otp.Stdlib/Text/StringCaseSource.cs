namespace Erlang;

internal sealed record StringCaseSource(
    string Function,
    string ListHelper,
    string BinaryHelper,
    int ListCall,
    int BinaryCall,
    int PublicList,
    int PublicBinary,
    int PublicEncode,
    int Error
)
{
    internal const int TitleHead = 697;
    internal const int TitleList = 698;
    internal const int TitleBinary = 703;
    internal const int ListAsciiChangedOffset = -10;
    internal const int ListAsciiUnchangedOffset = -6;
    internal const int ListUnchangedOffset = 1;
    internal const int ListChangedOffset = 2;
    internal const int BinaryAsciiChangedOffset = -5;
    internal const int BinaryAsciiUnchangedOffset = -2;
    internal const int BinaryUnchangedCpOffset = 2;
    internal const int BinaryChangedCpOffset = 13;
    internal const int BinaryUnchangedRecursionOffset = 4;
    internal const int BinaryChangedRecursionOffset = 15;
    internal const int BinaryUnchangedErrorOffset = 10;
    internal const int BinaryChangedErrorOffset = 19;
    internal static readonly StringCaseSource Upper = new(
        StringFunctionNames.Uppercase,
        "uppercase_list",
        "uppercase_bin",
        1317,
        1330,
        639,
        643,
        644,
        650
    );
    internal static readonly StringCaseSource Lower = new(
        StringFunctionNames.Lowercase,
        "lowercase_list",
        "lowercase_bin",
        1366,
        1379,
        670,
        674,
        675,
        681
    );
    internal static readonly StringCaseSource Fold = new(
        StringFunctionNames.Casefold,
        "casefold_list",
        "casefold_bin",
        1415,
        1428,
        726,
        730,
        731,
        737
    );
}
