namespace Erlang.Tests;

internal static class ResourceBinary577TestNames
{
    internal const string BoundaryCase = "runtime/resource-bits577/canonical-part-and-public-reregister-opaque";
    internal const string GuestModule = "resource_bits_guest577", GuestPart = "part", Part = "part", Functions = "functions";
    internal const string GuestSource = "-module(resource_bits_guest577). -export([part/1]). part(B) -> byte_size(binary:part(B,1,65)).";
    internal const string HeaderCase = "runtime/resource-bits577/actual-sub-bits-binref-readers-and-corruption";
    internal const string NestedRecipeCase = "runtime/resource-bits583/nested-portable-recipe-keeps-original-binref";
    internal const string SharingCase = "runtime/resource-bits577/slice-recipe-shared-binref-minor-major";
    internal const string EscapeCase = "runtime/resource-bits577/unpublished-rollback-host-export-and-stale-foreign";
    internal const string Heap = "TermHeap506", Enter = "Enter", Reserve = "Reserve", Read = "Read", Clear = "Clear";
    internal const string Adopt = "AdoptGuestInto507", SetLocal = "SetLocalGraph507", Collect = "Collect", CollectGeneration = "CollectGeneration508";
    internal const string Storage = "OwnedStorage577", Slab = "Slab", Cells = "Cells", Offset = "AllocationOffset526", Gate = "Gate";
    internal const string ReadHeader = "ReadHeader526", ReadNumeric = "ReadNumeric556", WriteHeaders = "WriteHeaders526", WriteNumeric = "WriteNumeric556";
    internal const string Snapshot = "PhysicalWordSnapshot526", Physical = "LivePhysicalWords";
    internal const string DomainSnapshot = "Snapshot524", LiveBytes = "LiveNativeBytes", LiveCount = "LiveResources";
    internal const string ReleaseNative = "ReleaseNative524", Retired = "Retired524", RetireImage = "RetireAsync";
    internal const string Domain = "NativeResources524", Binary = "Binary524", Pointer = "Pointer524";
    internal const string Node = "TryGetResourceBinaryNode577", NodeBlock = "Block", NodeAddress = "Address";
    internal const string ReadSize = "TryReadPhysicalSize526";
    internal const string Export = "Export", BeginConstruction = "BeginConstruction514", Create = "CreateResourceBits577";
    internal const string AdmitHost = "AdmitCurrent524", ReadBits = "ReadResourceBits577";
    internal const string Failure = "The actual resource BinRef/SubBits storage or lifetime control failed.";
    internal const string CleanupFailure = "A resource-bits577 assertion and/or independent cleanup phase failed.";
    internal const int Bytes = 95;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
