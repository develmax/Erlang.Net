namespace Erlang;

internal static class StringModernSource
{
    internal const int GraphemesCall = 277;
    internal const int GraphemesRecursive = 278;
    internal const int GraphemesError = 280;
    internal const int LengthCall = 1146;
    internal const int LengthError = 1149;
    internal const int ReverseCall = 1209;
    internal const int ReverseError = 1212;
    internal const int LengthBinaryGc = 1163;
    internal const int LengthBinaryCp = 1164;
    internal const int LengthBinaryError = 1166;
    internal const int SliceTrailKeepCall = 1265;
    internal const int ReverseBinaryGc = 1219;
    internal const int ReverseBinaryCp = 1220;
    internal const int ReverseBinaryError = 1223;
    internal const int SliceTwoHead = 376;
    internal const int SliceBinaryDropGc = 1251;
    internal const int SliceBinaryDropCp = 1253;
    internal const int SliceBinaryDropError = 1256;
    internal const int SliceBinaryGc = 1295;
    internal const int SliceBinaryCp = 1296;
    internal const int SliceBinaryError = 1299;
    internal const int SliceHead = 403;
    internal const int SliceDropCall = 405;
    internal const int SliceInfinityDropCall = 410;
    internal const int SliceTwoDropCall = 377;
    internal const int SliceDropGc = 1235;
    internal const int SliceDropError = 1238;
    internal const int SliceTrailHead = 1262;
    internal const int SliceTrailError = 1270;
    internal const int SliceListCall = 1280;
    internal const int SliceListError = 1283;
    internal const int SliceListAsciiRecursive = 1278;
    internal const int SliceListRecursive = 1281;
    internal const int PadHead = 459;
    internal const int PadLeadingLength = 460;
    internal const int PadTrailingLength = 463;
    internal const int PadBothLength = 466;
    internal static readonly StringSourceStep Length = new(
        StringSourceNames.LengthList,
        2,
        LengthCall,
        LengthError
    );
    internal static readonly StringSourceStep Reverse = new(
        StringSourceNames.ReverseList,
        2,
        ReverseCall,
        ReverseError
    );
    internal static readonly StringSourceStep Graphemes = new(
        StringFunctionNames.ToGraphemes,
        1,
        GraphemesCall,
        GraphemesError
    );
    internal static readonly StringSourceStep SliceDrop = new(
        StringSourceNames.SliceDropList,
        2,
        SliceDropGc,
        SliceDropError
    );
    internal static readonly StringSourceStep SliceList = new(
        StringSourceNames.SliceList,
        2,
        SliceListCall,
        SliceListError
    );
}
