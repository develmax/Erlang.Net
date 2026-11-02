namespace Erlang;

// Binding data only: this is not a native-success or publication-authority token.
// The owning reservation controls retirement and actual owner transfer.
internal sealed class NifPreparedImageBindings544
{
    private readonly NifImageBindings owner;
    private readonly NifImageBindingReservation reservation;
    private Dictionary<(string Name, int Arity), ErlangFunction>? snapshot;

    private NifPreparedImageBindings544(
        NifImageBindings owner,
        NifImageBindingReservation reservation,
        Dictionary<(string Name, int Arity), ErlangFunction> snapshot,
        Term installedFunctions
    )
    {
        this.owner = owner;
        this.reservation = reservation;
        this.snapshot = snapshot;
        InstalledFunctions544 = installedFunctions;
    }

    internal static NifPreparedImageBindings544 Create544(
        NifImageBindings owner,
        NifImageBindingReservation pending,
        IEnumerable<KeyValuePair<(string Name, int Arity), ErlangFunction>> bindings
    )
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var snapshot = new Dictionary<(string Name, int Arity), ErlangFunction>();
        var ordered = new List<Term>();
        foreach (var binding in bindings)
        {
            if (binding.Value is null || !pending.Eligible.Contains(binding.Key))
                throw new ArgumentException(NifImageBindingNames.InvalidFunction, nameof(bindings));
            if (!snapshot.TryAdd(binding.Key, binding.Value))
                throw new ArgumentException(NifImageBindingNames.DuplicateFunction, nameof(bindings));
            ordered.Add(Term.Tuple(Term.A(binding.Key.Name), Term.I(binding.Key.Arity)));
        }
        Term installed = Cons.From(ordered);

        return new NifPreparedImageBindings544(
            owner,
            pending,
            snapshot,
            installed
        );
    }

    internal Term InstalledFunctions544 { get; }

    // The table checks its live reservation under its own gate before consuming.
    // No enumerable, delegate factory, atom/list builder or owner callback runs here.
    internal Dictionary<(string Name, int Arity), ErlangFunction> Consume544(
        NifImageBindings selectedOwner,
        NifImageBindingReservation selectedReservation
    )
    {
        if (!ReferenceEquals(owner, selectedOwner) || !ReferenceEquals(reservation, selectedReservation))
            throw new InvalidOperationException(NifImageBindingNames.ForeignReservation);

        return Interlocked.Exchange(ref snapshot, null)
            ?? throw new InvalidOperationException(NifImageBindingNames.ForeignReservation);
    }
}
