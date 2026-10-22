using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class GuestStoredFunction407Access
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(GuestStoredFunction407Names.Failure);
    }

    internal static FunctionTerm Copy(
        object storage,
        ProcessContext context,
        Term table,
        FunctionTerm function
    )
    {
        long slot = EtsFoundation405Access.Store(
            storage,
            context,
            table,
            Term.Tuple(function)
        );

        return BeamStoredFunction406Access.Read(
            storage,
            context,
            table,
            slot
        );
    }

    internal static void Equal(FunctionTerm source, FunctionTerm copy, Pid owner)
    {
        var options = new EtfEncodingOptions { FunctionOwner = owner };
        Require(source.Equals(copy) && source.GetHashCode() == copy.GetHashCode()
            && ExternalTermFormat.Encode(source, options).SequenceEqual(ExternalTermFormat.Encode(copy, options)));
    }

    internal static void Refused(
        object storage,
        ProcessContext context,
        Term table,
        FunctionTerm function
    )
    {
        var before = EtsFoundation405Access.Snapshot(storage);
        bool refused = false;
        try
        {
            EtsFoundation405Access.Store(
                storage,
                context,
                table,
                Term.Tuple(function)
            );
        }
        catch (NotSupportedException failure) when (failure.Message == EtsFoundation405Names.UnsupportedFunction)
        {
            refused = true;
        }
        Require(refused && before.Equals(EtsFoundation405Access.Snapshot(storage)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Alive(WeakReference<object> value) => value.TryGetTarget(out _);

    internal static void Collected(WeakReference<object> value)
    {
        for (int index = 0; index < BeamStoredFunction406Names.CollectionAttempts && Alive(value); index++)
            EtsFoundation405Access.Collect();
        Require(!Alive(value));
    }
}
