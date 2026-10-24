using System.Reflection;
namespace Erlang.Tests;

internal static class CountersHost421Names
{
    internal const string ValuesTest = "HOST/counters421/owned-native-values-validation-and-foreign-kind";
    internal const string ConcurrencyTest = "HOST/counters421/real-lane-add-and-base-put";
    internal const string GcTest = "HOST/counters421/actual-gc-retires-unreachable-native-array";
    internal const string CloseTest = "HOST/counters421/all-three-shutdown-paths-join-real-leases";
    internal const string UnusedInitializerTest = "HOST/counters421/unused-owner-initializer-death-closes-admission-before-disposed";
    internal const string Failure = "Actual counter owner, numeric or lifecycle invariant failed";
    internal const string New = "counters_new", Get = "counters_get", Put = "counters_put", Add = "counters_add", Info = "counters_info";
    internal const string ErLang = "erlang", Internal = "erts_internal", ProcessFlag = "process_flag", Scheduler = "scheduler";
    internal const string Encode = "term_to_binary", Decode = "binary_to_term", Memory = "memory";
    internal const string Owner = "counters421", Enter = "Enter", Entry = "Entry", Read = "Read", RootType = "Erlang.CountersGuestRoot421", Array = "Array", Acquire = "Acquire";
    internal const string Kill = "kill";
    internal const string RuntimeGate = "gate", OwnerProperty = "CountersOwner421";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const int Lanes = 2, Workers = 6, Iterations = 500;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
