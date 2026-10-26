namespace Erlang.NifNativeBackend;

internal static class NifTextBinaryCallbackNames
{
    internal const string Mode = "--term-callback-fixture";
    internal const string Admission = "ROOT_OWN_TEXT_BINARY_CALLBACK_FIXTURE_ADMISSION";
    internal const string Build = "PASS_OWNED_WINDOWS_NATIVE_ENTRY_FIXTURE_BUILD";
    internal const string Platform = "windows-x64-llp64";
    internal const string Module = "text_binary_callback_contract";
    internal const string Load = "load";
    internal const string Inspect = "inspect";
    internal const string InspectNull = "inspect_null";
    internal const string Arrays = "arrays";
    internal const string Ok = "ok";
    internal const string Result = "text_binary_callback_contract";
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
    internal const string Run = "run";
    internal static readonly string[] Cases = ["tuple_access", "list_length", "atom_latin1", "atom_utf8", "atom_length_latin1", "atom_length_utf8", "string_latin1", "string_utf8", "atom_make", "atom_existing", "string_make", "latewrite_63_64_65", "alloc_realloc_transfer", "readonly_realloc", "inspect_iolist", "release", "pending_exception_latewrite", "iolist_snapshot_latewrite", "native_alloc_first_call", "native_transfer_next_call", "legacy_utf8", "legacy_invalid_utf8", "legacy_long_latin1", "legacy_long_utf8"];
}
