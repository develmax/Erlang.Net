namespace Erlang.Tests;

internal static class ResourcePolicy508TestNames
{
    internal const string TableTest = "runtime/resource-policy508/pinned-size-table-and-original-137-classes";
    internal const string DefaultsTest = "runtime/resource-policy508/vm-default-snapshots-and-distinct-sweep-conversions";
    internal const string GenerationsTest = "runtime/resource-policy508/actual-young-old-slab-reclamation";
    internal const string ClassesType = "Erlang.PinnedHeapSizeClasses508";
    internal const string PolicyType = "Erlang.ProcessResourcePolicy508";
    internal const string DefaultsType = "Erlang.RuntimeResourceDefaults508";
    internal const string ParserType = "Erlang.ProcessResourcePolicyParser508";
    internal const string Next = "Next";
    internal const string Minimum = "Minimum";
    internal const string Sweep = "SystemSweep";
    internal const string Apply = "TryApplySpawnOption";
    internal const string Snapshot = "Snapshot";
    internal const string ExchangeHeap = "ExchangeMinimumHeap";
    internal const string MinimumHeap = "MinimumHeapWords";
    internal const string FullSweep = "FullSweepAfter";
    internal const string HeapFlag = "min_heap_size";
    internal const string SweepFlag = "fullsweep_after";
    internal const string Heap = "TermHeap506";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string ContextCollection = "CollectOwnedGeneration508";
    internal const string CollectionPolicy = "GarbageCollectionPolicy";
    internal const string ExchangeThreshold = "ExchangeThreshold";
    internal const string Collection = "CollectGeneration508";
    internal const string Major = "Major";
    internal const string GenerationSnapshot = "GenerationSnapshot508";
    internal const string Nursery = "NurseryPayloadWords";
    internal const string Survivor = "SurvivorPayloadWords";
    internal const string Old = "OldPayloadWords";
    internal const string Reclaimed = "ReclaimedPayloadWords";
    internal const string Failure = "Resource policy508 source-only integration control failed.";
    // Actual passive ETF observation from pinned OTP provider, not CLR guesses.
    internal const ulong ObservedMinimumHeap = 233;
    internal const ulong ObservedMinimumBinaryHeap = 46422;
    internal static readonly ulong[] ObservedHeapClasses64 =
    [
        12UL, 38UL, 51UL, 90UL, 142UL,
        233UL, 376UL, 610UL, 987UL, 1598UL,
        2586UL, 4185UL, 6772UL, 10958UL, 17731UL,
        28690UL, 46422UL, 75113UL, 121536UL, 196650UL,
        318187UL, 514838UL, 833026UL, 999631UL, 1199557UL,
        1439468UL, 1727361UL, 2072833UL, 2487399UL, 2984878UL,
        3581853UL, 4298223UL, 5157867UL, 6189440UL, 7427328UL,
        8912793UL, 10695351UL, 12834421UL, 15401305UL, 18481566UL,
        22177879UL, 26613454UL, 31936144UL, 38323372UL, 45988046UL,
        55185655UL, 66222786UL, 79467343UL, 95360811UL, 114432973UL,
        137319567UL, 164783480UL, 197740176UL, 237288211UL, 284745853UL,
        341695023UL, 410034027UL, 492040832UL, 590448998UL, 708538797UL,
        850246556UL, 1020295867UL, 1224355040UL, 1469226048UL, 1763071257UL,
        2115685508UL, 2538822609UL, 3046587130UL, 3655904556UL, 4387085467UL,
        5264502560UL, 6317403072UL, 7580883686UL, 9097060423UL, 10916472507UL,
        13099767008UL, 15719720409UL, 18863664490UL, 22636397388UL, 27163676865UL,
        32596412238UL, 39115694685UL, 46938833622UL, 56326600346UL, 67591920415UL,
        81110304498UL, 97332365397UL, 116798838476UL, 140158606171UL, 168190327405UL,
        201828392886UL, 242194071463UL, 290632885755UL, 348759462906UL, 418511355487UL,
        502213626584UL, 602656351900UL, 723187622280UL, 867825146736UL, 1041390176083UL,
        1249668211299UL, 1499601853558UL, 1799522224269UL, 2159426669122UL, 2591312002946UL,
        3109574403535UL, 3731489284242UL, 4477787141090UL, 5373344569308UL, 6448013483169UL,
        7737616179802UL, 9285139415762UL, 11142167298914UL, 13370600758696UL, 16044720910435UL,
        19253665092522UL, 23104398111026UL, 27725277733231UL, 33270333279877UL, 39924399935852UL,
        47909279923022UL, 57491135907626UL, 68989363089151UL, 82787235706981UL, 99344682848377UL,
        119213619418052UL, 143056343301662UL, 171667611961994UL, 206001134354392UL, 247201361225270UL,
        296641633470324UL, 355969960164388UL, 427163952197265UL, 512596742636718UL, 615116091164061UL,
        738139309396873UL, 885767171276247UL,
    ];
}
