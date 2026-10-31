namespace Erlang;

internal sealed partial class RuntimeOsEnvironment : IDisposable
{
    private readonly object gate = new();
    private readonly WindowsOsEnvironmentLocale locale;
    private WindowsOsEnvironmentTree tree;
    private bool closed;

    private RuntimeOsEnvironment(IReadOnlyList<KeyValuePair<string, string>> initial)
    {
        locale = new();
        try
        {
            tree = new(locale);
            foreach (var entry in initial) tree.Put(entry.Key, entry.Value);
        }
        catch
        {
            locale.Dispose();
            throw;
        }
    }

    internal static RuntimeOsEnvironment CaptureWindows() => new(WindowsOsEnvironmentSnapshot.Capture());

    private void RequireOpen() => ObjectDisposedException.ThrowIf(closed, this);

    internal Term Get(Term key, CancellationToken cancellation)
    {
        string name = WindowsOsEnvironmentCodec.Read(key, true, cancellation);
        lock (gate)
        {
            RequireOpen();

            return tree.Get(name) is { } value ? WindowsOsEnvironmentCodec.Write(value, cancellation) : Term.A(CommonAtomNames.False);
        }
    }

    internal Term Put(Term key, Term value, CancellationToken cancellation)
    {
        string name = WindowsOsEnvironmentCodec.Read(key, true, cancellation);
        string text = WindowsOsEnvironmentCodec.Read(value, false, cancellation);
        lock (gate)
        {
            RequireOpen();
            cancellation.ThrowIfCancellationRequested();
            tree.Put(name, text);

            return Term.A(CommonAtomNames.True);
        }
    }

    internal Term Unset(Term key, CancellationToken cancellation)
    {
        string name = WindowsOsEnvironmentCodec.Read(key, true, cancellation);
        lock (gate)
        {
            RequireOpen();
            cancellation.ThrowIfCancellationRequested();
            tree.Remove(name);

            return Term.A(CommonAtomNames.True);
        }
    }

    internal Term All(CancellationToken cancellation)
    {
        lock (gate)
        {
            RequireOpen();
            Term result = Nil.Value;
            foreach (var entry in tree.PostOrder(cancellation))
                result = new Cons(
                    Term.Tuple(WindowsOsEnvironmentCodec.Write(entry.Key, cancellation), WindowsOsEnvironmentCodec.Write(entry.Value, cancellation)),
                    result
                );

            return result;
        }
    }

    internal IReadOnlyList<KeyValuePair<string, string>> PortSnapshot(CancellationToken cancellation)
    {
        lock (gate)
        {
            RequireOpen();

            return tree.PostOrder(cancellation);
        }
    }

    internal IReadOnlyList<KeyValuePair<string, string>> PortOverlay(
        IReadOnlyList<KeyValuePair<string, string>> baseline,
        IReadOnlyList<KeyValuePair<string, string?>> replacements,
        CancellationToken cancellation
    )
    {
        lock (gate)
        {
            RequireOpen();
            var local = new WindowsOsEnvironmentTree(locale);
            foreach (var entry in baseline)
            {
                cancellation.ThrowIfCancellationRequested();
                local.Put(entry.Key, entry.Value);
            }
            foreach (var entry in replacements)
            {
                cancellation.ThrowIfCancellationRequested();
                if (entry.Value is { } value) local.Put(entry.Key, value); else local.Remove(entry.Key);
            }

            return local.PostOrder(cancellation);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (closed) return;
            closed = true;
            if (driverLeases422 == 0) ReleaseDriverStorage422();
        }
    }
}
