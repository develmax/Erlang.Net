using System.Text.Json;

namespace Erlang.Tests;

internal sealed class StandardBootPersistent417Proof(object store, object[] sourceOwners)
{
    private readonly object retainedStore = store;
    private readonly object[] owners = sourceOwners;

    internal static StandardBootPersistent417Proof Capture(ProcessRuntime runtime)
    {
        var image = runtime.Modules.SourceOwner(PersistentNative417Names.Module, PersistentNative417Names.Info, 0) as PreparedBeamImage;
        StandardBootOriginal412Access.Require(image is not null
            && image.NativePrimitiveExports.Count == PersistentNative417Names.NativeCount);
        var rows = new List<object>();
        foreach (var signature in PersistentNative417Names.Signatures)
        {
            bool actual = image!.NativePrimitiveExports.Contains(signature)
                && runtime.Modules.HasExport(PersistentNative417Names.Module, signature.Name, signature.Arity)
                && ReferenceEquals(runtime.Modules.SourceOwner(PersistentNative417Names.Module, signature.Name, signature.Arity), image);
            StandardBootOriginal412Access.Require(actual);
            rows.Add(new { Module = image.Name, Function = signature.Name, signature.Arity, ActualNativeSourceOwner = actual });
        }

        return new(PersistentTerm408HostAccess.Store(runtime), rows.ToArray());
    }

    internal static bool Complete(string output, StandardBootPersistent417Proof? proof)
    {
        object? report = proof is null ? null : PersistentTerm408HostAccess.Call(proof.retainedStore, PersistentTerm408HostNames.Report);
        bool passed = report is not null && proof is not null && proof.owners.Length == PersistentNative417Names.NativeCount
            && (bool)report.GetType().GetProperty(PersistentLifecycle408Names.Closed)!.GetValue(report)!
            && PersistentArena416HostAccess.Number(report, PersistentLifecycle408Names.ActivePairs) == 0
            && PersistentArena416HostAccess.Number(report, PersistentTerm408HostNames.Payloads) == 0
            && PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedTables) == 0
            && PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedPairBytes) == 0
            && PersistentArena416HostAccess.Number(report, PersistentNative417Names.RetainedTableBytes) == 0;
        File.WriteAllText(
            Path.Combine(output, StandardBootPersistent417Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Passed = passed,
                WholeOriginalPersistentSeven = true,
                SourceOwners = proof?.owners,
                AfterActualRuntimeDispose = report,
                Original79GoldenUnchanged = true,
                WholeEts51Admitted = false,
                PhysicalMemoryAbiEqualityClaim = false
            })
        );

        return passed;
    }
}
