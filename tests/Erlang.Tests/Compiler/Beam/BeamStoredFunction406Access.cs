using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class BeamStoredFunction406Access
{
    // Probe actual table-held recipe/identity custody, not a detached public
    // input wrapper or a separate closure binding captured before storage.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (WeakReference<object> Binding, WeakReference<object> Resource) StoredRoots515(
        ProcessRuntime runtime,
        object storage,
        Term table,
        long? objectId = null
    )
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        object? Field(object target, string name) => target.GetType().GetField(name, members)!.GetValue(target);
        lock (typeof(ProcessRuntime).GetField(BeamStoredFunction406Names.RuntimeGate515, members)!.GetValue(runtime)!)
        {
            object resolved = EtsFoundation405Access.Invoke(storage, BeamStoredFunction406Names.Resolve515, table)!;
            var slots = ((System.Collections.IEnumerable)Property(resolved, BeamStoredFunction406Names.Rows515)).Cast<object>();
            object slot = objectId is { } selected
                ? slots.Single(item => (long)Property(item, BeamStoredFunction406Names.Id515) == selected)
                : slots.Single();
            object payload = Field(Property(slot, BeamStoredFunction406Names.Payload515), BeamStoredFunction406Names.PayloadField515)!;
            var owners = new List<object>();
            var identities = new List<object>();
            if (Field(payload, BeamStoredFunction406Names.Compressed515) is { } compressed)
            {
                owners.AddRange(((System.Collections.IEnumerable)Property(compressed, BeamStoredFunction406Names.Functions515)).Cast<object>());
                identities.AddRange(((System.Collections.IEnumerable)Property(compressed, BeamStoredFunction406Names.Identities515)).Cast<object>());
            }
            else
            {
                foreach (object node in (System.Collections.IEnumerable)Field(payload, BeamStoredFunction406Names.Nodes515)!)
                {
                    if (Property(node, BeamStoredFunction406Names.Function515) is { } recipe) owners.Add(recipe);
                    if (Property(node, BeamStoredFunction406Names.Identity515) is { } identity) identities.Add(identity);
                }
            }
            object owner = owners.Single();
            object retainedOwner = owner.GetType().GetProperty(BeamStoredFunction406Names.Binding515, members)?.GetValue(owner) ?? owner;
            object retainedResource = identities.Select(identity => Property(identity, BeamStoredFunction406Names.Value515))
                .OfType<ResourceReferenceTerm>().Single();

            return (new(retainedOwner), new(retainedResource));
        }
    }

    // Public copy_struct does not promise to preserve DAG sharing. Rebind an
    // authenticated runtime recipe to an explicit shared input before testing
    // the independent storage copier's sharing and ownership contracts.
    internal static FunctionTerm RebindEnvironment521(
        ProcessRuntime runtime,
        FunctionTerm function,
        Term environment
    )
    {
        object recipe;
        try
        {
            recipe = typeof(ProcessRuntime).Assembly.GetType(BeamStoredFunction406Names.StoredRecipes521, true)!
                .GetMethod(BeamStoredFunction406Names.CaptureRecipe521, BeamStoredFunction406Names.Static)!
                .Invoke(null, [runtime, function])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
        var rebound = (FunctionTerm)EtsFoundation405Access.Invoke(
            recipe,
            BeamStoredFunction406Names.RebindRecipe521,
            (object)new Term[] { environment }
        )!;
        Require(rebound.Equals(function) && rebound.Arity == function.Arity
            && ReferenceEquals(rebound.Environment.Single(), environment));

        return rebound;
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BeamStoredFunction406Names.Failure);
    }

    internal static object Property(object target, string name)
        => target.GetType().GetProperty(name, BeamStoredFunction406Names.Members)!.GetValue(target)!;

    internal static object CallFactory(string name, params object[] arguments)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(BeamStoredFunction406Names.FactoryType, true)!
                .GetMethod(name, BeamStoredFunction406Names.Static)!.Invoke(null, arguments)!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
            throw;
        }
    }

    internal static byte[] Bytes()
    {
        Assembly assembly = typeof(BeamStoredFunction406Access).Assembly;
        string suffix = BeamClosureHostNames.ResourceSeparator + BeamClosureHostNames.Module + BeamClosureHostNames.BeamExtension;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamClosureHostNames.Hash);

        return bytes;
    }

    internal static PreparedBeamImage Publish(ProcessRuntime runtime)
    {
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(Bytes());
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);

        return prepared;
    }

    internal static int Active(ProcessContext context, ModuleCodeImage image)
        => ((Dictionary<ModuleCodeImage, int>)typeof(ProcessContext)
            .GetField(BeamStoredFunction406Names.ActiveCode, BeamStoredFunction406Names.Members)!
            .GetValue(context)!).GetValueOrDefault(image);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool Alive(WeakReference<object> weak) => weak.TryGetTarget(out _);

    internal static void RequireCollected(WeakReference<object> weak)
    {
        for (int index = 0; index < BeamStoredFunction406Names.CollectionAttempts && Alive(weak); index++)
            EtsFoundation405Access.Collect();
        Require(!Alive(weak));
    }

    internal static FunctionTerm Read(
        object store,
        ProcessContext context,
        Term table,
        long slot
    )
        => (FunctionTerm)((TupleTerm)EtsFoundation405Access.Read(
            store,
            context,
            table,
            slot
        )).Items[0];
}
