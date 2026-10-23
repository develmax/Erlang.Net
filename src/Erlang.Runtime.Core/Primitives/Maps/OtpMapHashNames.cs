namespace Erlang;

internal static class OtpMapHashNames
{
    internal const ulong FirstMultiplier = 0x87C37B91114253D5UL;
    internal const ulong SecondMultiplier = 0x4CF5AD432745937FUL;
    internal const ulong FirstAddend = 0x52DCE729UL;
    internal const ulong SecondAddend = 0x38495AB5UL;
    internal const ulong FinalFirstMultiplier = 0xFF51AFD7ED558CCDUL;
    internal const ulong FinalSecondMultiplier = 0xC4CEB9FE1A85EC53UL;
    internal const ulong SmallTag = 15;
    internal const ulong NilWord = 59;
    internal const int AtomShift = 6;
    internal const ulong AtomTag = 11;
    internal const string BadArgument = "badarg";
    internal const int SmallShift = 4;
    internal const int SmallPayloadBits = 60;
    internal const int Immediate = 1;
    internal const int ArrayElement = 2;
    internal const int Car = 3;
    internal const int Cdr = 4;
    internal const int String = 5;
    internal const int Tuple = 6;
    internal const int Record = 7;
    internal const int FlatMap = 8;
    internal const int HashMapHeadArray = 9;
    internal const int HashMapHeadBitmap = 10;
    internal const int HashMapNode = 11;
    internal const int Binary = 12;
    internal const int LocalFun = 13;
    internal const int ExternalFun = 14;
    internal const int NegativeBig = 15;
    internal const int PositiveBig = 16;
    internal const int LocalReference = 17;
    internal const int ExternalReference = 18;
    internal const int ExternalPid = 19;
    internal const int ExternalPort = 20;
    internal const int Float = 21;
    internal const int WordBits = 64;
    internal const int HalfWordBits = 32;
    internal const int WordBytes = 8;
    internal const int BinaryBlockBytes = 16;
    internal const int StringGroupBytes = 4;
    internal const int ByteBits = 8;
}
