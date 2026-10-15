using Erlang;
using System.Runtime.CompilerServices;

namespace Erlang.NifFixtureHost;

internal sealed class NifGenerationLeaseTable
{
    private readonly object gate = new();
    private readonly Dictionary<ModuleCodeImage, NifGenerationState> states = new();
    private readonly ConditionalWeakTable<ModuleCodeImage, object> retiredImages = new();

    internal int RetainedGenerationCount
    {
        get
        {
            lock (gate)

                return states.Count;
        }
    }

    internal void Bind(
        ModuleCodeImage image,
        object owner,
        Action unload
    )
    {
        lock (gate)
        {
            if (retiredImages.TryGetValue(image, out _))
                throw new ObjectDisposedException(NifHostDiagnostics.RetiredGenerationBinding);
            states.Add(image, new(owner, unload));
        }
    }

    internal IDisposable Acquire(
        ProcessContext caller,
        ModuleCodeImage image,
        bool requireCurrent = true
    )
    {
        caller.Cancellation.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (!states.TryGetValue(image, out var state) || state.Retired)
                throw new ObjectDisposedException(NifHostDiagnostics.RetiredGenerationAcquisition);
            if (!ReferenceEquals(caller.ExecutingSourceImage(state.Owner), image))
                throw new InvalidOperationException(NifHostDiagnostics.ForeignSourceOwner);
            if (requireCurrent && !ReferenceEquals(caller.Runtime.Modules.CurrentCodeImage(image.Module), image))
                throw new InvalidOperationException(NifHostDiagnostics.OldGenerationLoad);
            state.Active++;

            return new NifGenerationLease(this, image);
        }
    }

    internal void Retire(ModuleCodeImage image)
    {
        Action? unload;
        lock (gate)
        {
            if (!states.TryGetValue(image, out var state))
                return;
            state.Retired = true;
            retiredImages.GetValue(image, static _ => new object());
            unload = TakeUnload(image, state);
        }
        unload?.Invoke();
    }

    internal void Release(ModuleCodeImage image)
    {
        Action? unload;
        lock (gate)
        {
            var state = states[image];
            if (state.Active <= 0)
                throw new InvalidOperationException(NifHostDiagnostics.UnbalancedGenerationLease);
            state.Active--;
            unload = TakeUnload(image, state);
        }
        unload?.Invoke();
    }

    private Action? TakeUnload(ModuleCodeImage image, NifGenerationState state)
    {
        if (!state.Retired || state.Active != 0 || state.Unloaded)

            return null;
        state.Unloaded = true;
        states.Remove(image);

        return state.Unload;
    }
}
