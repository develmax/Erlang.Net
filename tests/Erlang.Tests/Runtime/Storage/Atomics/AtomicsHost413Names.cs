namespace Erlang.Tests;

internal static class AtomicsHost413Names
{
    internal const string AdmissionTest = "runtime/atomics413/original-whole7-admission-and-owned-values";
    internal const string ConcurrencyTest = "runtime/atomics413/interlocked-concurrent-distinct-tickets";
    internal const string LifetimeTest = "runtime/atomics413/creator-death-canonical-reference-and-foreign-domain";
    internal const string CloseTest = "runtime/atomics413/close-joins-real-invocation-and-array-leases";
    internal const string RollbackTest = "runtime/atomics413/constructor-rollback-registration-conflict-and-transaction";
    internal const string GcTest = "runtime/atomics413/gc-drops-guest-root-without-runtime-term-retention";
    internal const string Prefix = "Erlang.Tests.AtomicsNative413.", Manifest = "manifest.json";
    internal const string Atomics = "atomics", Internal = "erts_internal", Runtime = "erlang", New = "new", Constructor = "atomics_new";
    internal const string Put = "put", Get = "get", AddGet = "add_get", Info = "info", Encode = "term_to_binary", Decode = "binary_to_term";
    internal const string File = "prim_file";
    internal const System.Reflection.BindingFlags Members = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    internal const string Memory = "memory", Beam = ".beam", Lists = "lists", Maps = "maps";
    internal const string Entry = "Entry", Read = "Read", RootType = "Erlang.AtomicsGuestRoot413", Array = "Array", Acquire = "Acquire";
    internal const string Owner = "atomics413", Enter = "Enter", Close = "Close";
    internal const string Failure = "Atomics original storage/lifetime control failed";
    internal const string Rollback = "atomics413-owned-registration-rollback";
    internal const string MagicHeaderType586 = "Erlang.AtomicsMagicHeader413";
    internal const string DestructorField586 = "Destructor";
    internal const int Natives = 7, Exports = 12, Workers = 4, Iterations = 500;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
