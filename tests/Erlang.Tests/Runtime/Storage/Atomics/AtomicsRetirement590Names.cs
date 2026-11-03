using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsRetirement590Names
{
    internal const string Test = "runtime/atomics590/post-free-error-forgets-reference-and-accounts-once";
    internal const string Failure = "Post-free atomics failure skipped reference retirement or physical accounting.";
    internal const string Owner = "AtomicsOwner413", Create = "Create", Enter = "Enter", Retire = "Retire";
    internal const string Bind = "BindOwner586", Memory = "memory", Header = "Erlang.AtomicsMagicHeader413", Destructor = "Destructor";
    internal const string Backing = "Erlang.AtomicsGuestRoot413", Registry = "ReferenceResources", Publish = "CreateAtomics413";
    internal const string IdentityIndex = "atomicsEntries586", RegistryGate = "gate";
    internal const string Acquire = "AcquireAtomics413", AllocationLive = "IsAllocated434", CloseFailure = "closeFailure", Retired = "Retired586";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
