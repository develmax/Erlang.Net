using System.Reflection;

namespace Erlang.Tests;

internal static class PersistentNative417Names
{
    internal const string ImageTest = "HOST/persistent-native417/original-seven-source-image-info-arena";
    internal const string OwnerTest = "HOST/persistent-native417/info-real-owner-cancel-and-closed";
    internal const string HaltHeldTest = "HOST/persistent-native417/halt-joins-held-native-reader";
    internal const string InitHeldTest = "HOST/persistent-native417/initializer-down-joins-held-native-reader";
    internal const string AmbientTest = "HOST/persistent-native417/owner-worker-does-not-retain-first-handler-arguments";
    internal const string Handler = "persistent_owner_handler417";
    internal const string Ok = "ok";
    internal const string Module = "persistent_term";
    internal const string Image = "persistent_term.beam";
    internal const string Info = "info";
    internal const string Get = "get";
    internal const string Put = "put";
    internal const string PutNew = "put_new";
    internal const string Erase = "erase";
    internal const string Count = "count";
    internal const string Memory = "memory";
    internal const string ActiveTable = "ActiveTableRequestedBytes";
    internal const string ActivePair = "ActivePairRequestedBytes";
    internal const string RetainedTables = "RetainedTableBlocks";
    internal const string RetainedPairBytes = "RetainedPairNativeBytes";
    internal const string RetainedTableBytes = "RetainedTableNativeBytes";
    internal const string Type = "Erlang.PersistentTermNative";
    internal const string Capture = "TryCapture";
    internal const int NativeCount = 7;
    internal static readonly BeamFunctionSignature[] Signatures =
    [new(Get, 0), new(Get, 1), new(Get, 2), new(Put, 2), new(PutNew, 2), new(Erase, 1), new(Info, 0)];
    internal const int PollMilliseconds = 10;
    internal const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
}
