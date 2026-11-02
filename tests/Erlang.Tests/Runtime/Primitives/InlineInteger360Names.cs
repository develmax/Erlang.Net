using System.Reflection;

namespace Erlang.Tests;

internal static class InlineInteger360Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal const string TestModule = "inline_integer_literal360";
    internal const string Value = "value";
    internal const string Same = "same";
    internal const string OldKey = "inline360_old";
    internal const string NewKey = "inline360_new";
    internal const string SecondKey = "inline360_second";
    internal const string Erlang = "erlang";
    internal const string Put = "put";
    internal const string Data = "data";
    internal const string Gate = "gate";
    internal const string Storage = "OwnedStorage559";
    internal const string Owner = "Owner";
    internal const string Domain = "LiteralDomain800";
    internal const string ImageDomain = "LiteralDomain819";
    internal const string Retirement = "Retirement800";
    internal const string Heap = "TermHeap506";
    internal const string Collect = "CollectGeneration508";
    internal const string Purge = "PurgeModule";
    internal const string Prefix = "Erlang.Tests.NumericContract487.";
    internal const string V1 = "InlineInteger360V1.beam";
    internal const string V2 = "InlineInteger360V2.beam";
    internal const string Original = "InlineInteger360Original.etf";
    internal const string V1Hash = "38D8CEB637861285143BD93F55EC0FBF828769B80764E3C60EDFF1BE72955EC6";
    internal const string V2Hash = "A57E4CA787824B9CBDEEADA4D6ECD3D607CE13062687AA01092392327FFA32D5";
    internal const string OriginalHash = "75072C4F1E34A7B657A002FDD659272CD111E0E548BBD67B29263053CD5FDD1B";
    internal const string Failure = "Inline integer360 original outcome or actual image custody differs.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
