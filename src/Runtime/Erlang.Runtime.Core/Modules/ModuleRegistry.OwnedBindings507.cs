using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly Dictionary<(string Module, string Function, int Arity), OwnedBinding507> ownedBindings507 = new();
    private readonly ConditionalWeakTable<object, OwnedPreparedCapabilities507> preparedOwnedCapabilities507 = new();

    // Only reviewed compiler/runtime publication paths call these internal
    // capability registrations. Public Register/RegisterSource remain opaque.
    internal void RegisterCanonicalSource507(
        string module,
        string function,
        int arity,
        ErlangFunction body,
        object owner
    )
    {
        RegisterSource(
            module,
            function,
            arity,
            body,
            owner
        );
        lock (gate)
        {
            var key = (module, function, arity);
            if (owningRuntime?.OwnedTermsPilot506 == true && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && sourceOwners.TryGetValue(key, out var currentOwner)
                && ReferenceEquals(currentOwner, owner))
                ownedBindings507[key] = new(
                    body,
                    codeImages[module],
                    owner,
                    OwnedBindingKind507.CompilerGuest
                );
        }
    }

    internal void RegisterCanonicalDictionary507(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        Register(
            module,
            function,
            arity,
            body
        );
        lock (gate)
        {
            var key = (module, function, arity);
            if (owningRuntime?.OwnedTermsPilot506 == true && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[module],
                    null,
                    OwnedBindingKind507.LocalDictionary
                );
        }
    }

    internal void RegisterCanonicalRead507(
        string module,
        string function,
        int arity,
        ErlangFunction body
    )
    {
        Register(
            module,
            function,
            arity,
            body
        );
        lock (gate)
        {
            var key = (module, function, arity);
            if (owningRuntime?.OwnedTermsPilot506 == true && functions.TryGetValue(key, out var selected)
                && ReferenceEquals(selected, body) && !sourceOwners.ContainsKey(key))
                ownedBindings507[key] = new(
                    body,
                    codeImages[module],
                    null,
                    OwnedBindingKind507.PureAliasRead
                );
        }
    }

    internal IReadOnlyDictionary<ErlangFunction, OwnedBindingKind507> PreparedOwnedCapabilities507(object primitives)
        => preparedOwnedCapabilities507.TryGetValue(primitives, out var captured) ? captured.Entries
            : System.Collections.Frozen.FrozenDictionary<ErlangFunction, OwnedBindingKind507>.Empty;

    internal OwnedBindingKind507 NativeBorrowKind507(BeamExecutionIdentity execution, ErlangFunction body)
        => execution.Owner is PreparedBeamImage image && ReferenceEquals(image.Owner, this)
            && execution.OwnedNativeCapabilities507.TryGetValue(body, out var kind) ? kind : OwnedBindingKind507.Opaque;

    private void BindPreparedOwnedBodies507(PreparedBeamImage image)
    {
        if (owningRuntime?.OwnedTermsPilot506 != true)
            return;
        for (int index = 0; index < image.Code.Exports.Count; index++)
        {
            BeamLinkedFunction function = image.Code.Exports[index];
            var key = (image.Name, function.Name, function.Arity);
            ErlangFunction body = image.Bodies[index];
            if (!functions.TryGetValue(key, out var selected) || !ReferenceEquals(body, selected)
                || !ReferenceEquals(codeImages[image.Name], image.ExecutionIdentity.Image))
                throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);
            OwnedBindingKind507 kind;
            if (image.ExecutionIdentity.NativePrimitives.TryGetValue((function.Name, function.Arity), out var primitive))
                kind = NativeBorrowKind507(image.ExecutionIdentity, primitive);
            else
                // This body executes admitted BEAM instructions. Every local native
                // entry independently applies its exact captured primitive capability.
                kind = OwnedBindingKind507.PreparedGuest;
            // Retain exact canonical publication authority even when currently
            // opaque. Only a later genuine image-bound NIF may upgrade its OWN entry.
            ownedBindings507[key] = new(
                body,
                image.ExecutionIdentity.Image,
                image.ExecutionIdentity.Owner,
                kind
            );
        }
    }

    private OwnedBindingKind507 SelectedOwnedBinding507((string Module, string Function, int Arity) key, ErlangFunction body)
    {
        if (!ownedBindings507.TryGetValue(key, out var binding)
            || !ReferenceEquals(binding.Body, body)
            || !ReferenceEquals(binding.Image, codeImages.GetValueOrDefault(key.Module)))
            return OwnedBindingKind507.Opaque;
        if (binding.SourceOwner is not null && (!sourceOwners.TryGetValue(key, out var owner)
            || !ReferenceEquals(owner, binding.SourceOwner)))
            return OwnedBindingKind507.Opaque;

        if (binding.Kind == OwnedBindingKind507.Opaque && binding.SourceOwner is PreparedBeamImage
            && NifImageBindingStore.TryGetValue(binding.Image, out var native535)
            && native535!.HasGuestNative535(key.Function, key.Arity))
            return OwnedBindingKind507.PreparedGuest;

        return binding.Kind;
    }
}
