namespace Erlang.NifNativeBackend;

internal static class NifTermCallbackNames
{
    internal const string Mode = "--term-callback-fixture";
    internal const string Admission = "ROOT_OWN_TERM_CALLBACK_FIXTURE_ADMISSION";
    internal const string Build = "PASS_OWNED_WINDOWS_NATIVE_ENTRY_FIXTURE_BUILD";
    internal const string Platform = "windows-x64-llp64";
    internal const string Module = "term_callback_contract";
    internal const string Load = "load";
    internal const string Inspect = "inspect";
    internal const string InspectNull = "inspect_null";
    internal const string Arrays = "arrays";
    internal const string Ok = "ok";
    internal const string Result = "term_callback_contract";
    internal const string Cleanup = "cleanup";
    internal const string Retired = "native_image_retired";
    internal const string Raw = "returned.etf";
    internal const string LoadRaw = "load-result.etf";
    internal const string Completion = "completion.json";
    internal const string LibrarySuffix = ".dll";
    internal const string TermSuffix = ".etf";
    internal const string ArrayPrefix = "array-";
    internal const string Reviewed = "reviewed";
    internal const string Ordinary = "ordinary";
    internal const string ExternalNode = "abi_peer@fixture";
    internal const string Atom = "hello";
    internal const string HeadAtom = "a";
    internal const string TailAtom = "tail_atom";
    internal const string Record = "native_record";
    internal const string Payload = "payload";
    internal const string Closure = "closure";
    internal const string Broken = "Term callback fixture contract failed";
    internal const int SentinelHead = 101;
    internal const int SentinelTail = 202;
    internal const int LargeCount = 8193;
    internal static readonly string[] Cases = [
        "nil", "proper", "improper", "atom", "integer", "big_integer", "float",
        "tuple", "empty_tuple", "binary", "sub_binary", "bitstring", "closure",
        "exported_fun", "local_pid", "external_pid", "local_port", "external_port",
        "local_ref", "external_ref", "map", "conventional_record"
    ];
}
