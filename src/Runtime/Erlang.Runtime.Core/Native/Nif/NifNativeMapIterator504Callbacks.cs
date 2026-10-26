using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMapIterator504Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Create(
        nint env,
        nuint map,
        NifNativeMapIterator504* iterator,
        int entry
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(map, env);
            if (!owner.TryMapIteratorEntries504(owner.GetValue(map), out var entries))
                return NifNativeMapIterator504Names.NotPositioned;
            if (entry is not (NifNativeMapIterator504Names.First or NifNativeMapIterator504Names.Last))
                return NifNativeMapIterator504Names.NotPositioned;
            if (iterator == null)
                throw new InvalidOperationException(NifNativeMapIterator504Names.NullIterator);
            var state = new NifNativeMapIterator504State(map, entries);
            nuint identity = owner.AddMapIterator504(state);
            *iterator = new NifNativeMapIterator504
            {
                Map = map,
                Size = (nuint)entries.Count,
                Index = entry == NifNativeMapIterator504Names.First ? 1 : (nuint)entries.Count,
                State = identity
            };

            return NifNativeMapIterator504Names.Success;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Destroy(nint env, NifNativeMapIterator504* iterator)
    {
        try
        {
            var (owner, _) = Resolve(env, iterator);
            owner.RemoveMapIterator504(iterator->State, iterator->Map);
            iterator->State = 0;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsHead(nint env, NifNativeMapIterator504* iterator)
    {
        try
        {
            _ = Resolve(env, iterator);

            return Flag(iterator->Size == 0 || iterator->Index == 0);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsTail(nint env, NifNativeMapIterator504* iterator)
    {
        try
        {
            _ = Resolve(env, iterator);

            return Flag(iterator->Size == 0 || iterator->Index > iterator->Size);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Next(nint env, NifNativeMapIterator504* iterator)
    {
        try
        {
            _ = Resolve(env, iterator);
            if (iterator->Index <= iterator->Size)
                iterator->Index++;

            return Flag(iterator->Index <= iterator->Size);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Previous(nint env, NifNativeMapIterator504* iterator)
    {
        try
        {
            _ = Resolve(env, iterator);
            if (iterator->Index > 0)
                iterator->Index--;

            return Flag(iterator->Index > 0);
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetPair(
        nint env,
        NifNativeMapIterator504* iterator,
        nuint* key,
        nuint* value
    )
    {
        try
        {
            var (owner, state) = Resolve(env, iterator);
            if (iterator->Index == 0 || iterator->Index > iterator->Size)
                return NifNativeMapIterator504Names.NotPositioned;
            if (key == null || value == null)
                throw new InvalidOperationException(NifNativeMapIterator504Names.NullOutput);
            var pair = state.Pair(owner, checked((int)iterator->Index - 1));
            *key = pair.Key;
            *value = pair.Value;

            return NifNativeMapIterator504Names.Success;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return NifNativeMapIterator504Names.HostFailure;
        }
    }

    private static (NifNativeCallEnvironment Owner, NifNativeMapIterator504State State) Resolve(
        nint env,
        NifNativeMapIterator504* iterator
    )
    {
        if (iterator == null)
            throw new InvalidOperationException(NifNativeMapIterator504Names.NullIterator);
        var owner = NifNativeCallEnvironment.ResolveTermOwner(iterator->Map, env);
        var state = owner.GetMapIterator504(iterator->State, iterator->Map);
        if (iterator->Size != (nuint)state.Entries.Count || iterator->Index > iterator->Size + 1)
            throw new InvalidOperationException(NifNativeMapIterator504Names.InvalidState);

        return (owner, state);
    }

    private static int Flag(bool value)
        => value ? NifNativeMapIterator504Names.Success : NifNativeMapIterator504Names.NotPositioned;
}
