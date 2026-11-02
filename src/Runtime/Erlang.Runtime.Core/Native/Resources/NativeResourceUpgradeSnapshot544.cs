namespace Erlang;

// Snapshot only of the pre-callback named table, never of types created by C.
internal sealed class NativeResourceUpgradeSnapshot544
{
    private readonly NativeResourceLoadTransaction527 transaction;
    private readonly NativeResourceDomain524 domain;
    private readonly NifNativeResourceImage524 image;
    private readonly NifResourceImagePin524 loadingPin;
    private readonly NativeResourceType524[] holds;
    private readonly Dictionary<string, NativeResourceType524> named = new(StringComparer.Ordinal);
    private int retained, released;
    internal Exception?[] Failures544 { get; }
    internal int FailureCapacity544 => holds.Length;

    internal NativeResourceUpgradeSnapshot544(
        NativeResourceLoadTransaction527 transaction,
        NativeResourceDomain524 domain,
        NifNativeResourceImage524 image,
        NifResourceImagePin524 loadingPin,
        NativeResourceType524[] types
    )
    {
        this.transaction = transaction;
        this.domain = domain;
        this.image = image;
        this.loadingPin = loadingPin;
        holds = types;
        Failures544 = new Exception?[types.Length];
        // The source list can contain duplicate names. find_resource_type
        // selects its FIRST preexisting entry; do not invent deduplication.
        foreach (var type in types) named.TryAdd(type.Name, type);
    }

    internal void RetainUnderPublication544()
    {
        foreach (var type in holds)
        {
            type.RetainPending527();
            retained++;
        }
    }

    internal bool IsActiveFor544(
        NativeResourceLoadTransaction527 candidate,
        NativeResourceDomain524 candidateDomain,
        NifNativeResourceImage524 candidateImage
    ) => Volatile.Read(ref released) == 0
        && ReferenceEquals(transaction, candidate) && ReferenceEquals(domain, candidateDomain)
        && ReferenceEquals(image, candidateImage)
        && ReferenceEquals(transaction.UpgradeSnapshot544, this)
        && transaction.Loading527 && loadingPin.IsActiveFor524(image.Image.NifBindings);

    internal NativeResourceType524? FindPending544(string name)
    {
        if (!IsActiveFor544(transaction, domain, image))
            throw new InvalidOperationException(NativeResourceUpgradeSnapshotNames544.Authority);
        if (!named.TryGetValue(name, out var type)) return null;
        type.RetainPending527();

        return type;
    }

    // Always outside callback/name/binding writers. Every held type is attempted.
    internal void Release544()
    {
        if (Interlocked.Exchange(ref released, 1) != 0) return;
        for (int index = retained - 1; index >= 0; index--)
            try
            {
                holds[index].ReleasePending527();
            }
            catch (Exception failure)
            {
                Failures544[index] = failure;
            }
        retained = 0;
        named.Clear();
        Array.Clear(holds);
    }
}
