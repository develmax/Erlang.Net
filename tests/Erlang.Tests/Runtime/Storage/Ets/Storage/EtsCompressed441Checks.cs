using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static partial class EtsCompressed441Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static void Require([DoesNotReturnIf(false)] bool condition)
    {
        if (!condition) throw new InvalidOperationException(EtsCompressed441Names.Failure);
    }

    private static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object? Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target);

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsDeletion.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsMutations414.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsInfo420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string name,
        params Term[] args
    )
        => runtime.Modules.Call(
            context,
            EtsCompressed441Names.Module,
            name,
            args
        );

    private static Task<Term> Create(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, string kind)
        => actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCompressed441Names.New,
            Term.A(EtsCompressed441Names.Name),
            Term.List(
                Term.A(kind),
                Term.A(EtsCompressed441Names.Public),
                Term.A(EtsCompressed441Names.Compressed),
                Term.Tuple(Term.A(EtsCompressed441Names.KeyPosition), Term.I(EtsCompressed441Names.KeyField))
            )
        ));

    private static object Slot(object storage, Term table)
        => ((IEnumerable)Property(EtsFoundation405Access.Invoke(storage, EtsCompressed441Names.Resolve, table)!, EtsCompressed441Names.Rows)).Cast<object>().Single();

    internal static async Task Storage()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        foreach (string kind in EtsCompressed441Names.Kinds)
        {
            Term table = await Create(runtime, actor, kind);
            var bytes = new byte[EtsCompressed441Names.RepetitiveBytes];
            TupleTerm row = Term.Tuple(new BitString(bytes), Term.I(EtsCompressed441Names.KeyField));
            await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Insert,
                table,
                row
            ));
            ulong ownedBytes = await actor.Invoke(context =>
            {
                object payload = Field(Property(Slot(storage, table), EtsCompressed441Names.PayloadProperty), EtsCompressed441Names.PayloadField)!;
                object compressed = Field(payload, EtsCompressed441Names.CompressedField)!;
                Require(compressed is not null && Field(payload, EtsCompressed441Names.NodesField) is null);
                Require(((byte[])Property(compressed, EtsCompressed441Names.Encoded)).Length < bytes.Length);

                return Census(
                    runtime,
                    storage,
                    table,
                    compressed
                );
            });
            var reportedWords = (Integer)await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Info,
                table,
                Term.A(EtsCompressed441Names.Memory)
            ));
            Require(reportedWords.Value * IntPtr.Size >= ownedBytes
                && reportedWords.Value * IntPtr.Size - ownedBytes < IntPtr.Size);
            Require((await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Lookup,
                table,
                Term.I(EtsCompressed441Names.KeyField)
            ))).Equals(Term.List(row)));
            Require((await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Member,
                table,
                Term.I(EtsCompressed441Names.KeyField)
            ))).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Info,
                table,
                Term.A(EtsCompressed441Names.Compressed)
            ))).Equals(Term.A(ErlangBooleanAtoms.True)));
            if (kind == EtsCompressed441Names.Kinds[0])
            {
                Term replacement = Term.I(EtsCompressed441Names.RepetitiveBytes);
                await actor.InvokeAsync(async context => await Call(
                    runtime,
                    context,
                    EtsCompressed441Names.Update,
                    table,
                    Term.I(EtsCompressed441Names.KeyField),
                    Term.Tuple(Term.I(EtsCompressed441Names.ValueField), replacement)
                ));
                Require((await actor.InvokeAsync(async context => await Call(
                    runtime,
                    context,
                    EtsCompressed441Names.Lookup,
                    table,
                    Term.I(EtsCompressed441Names.KeyField)
                ))).Equals(Term.List(Term.Tuple(replacement, Term.I(EtsCompressed441Names.KeyField)))));
            }
            await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Delete,
                table
            ));
            EtsFoundation405Access.Empty(storage);
        }
    }

    private static ulong Census(
        ProcessRuntime runtime,
        object storage,
        Term id,
        object? compressed,
        int expectedBeamRecipes = 0
    )
    {
        var observed = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var measure = typeof(ProcessRuntime).Assembly.GetType(EtsCompressed441Names.ExtentType, true)!
            .GetMethod(EtsCompressed441Names.Measure, Members)!.CreateDelegate<Func<object, nuint>>();
        Func<object, nuint> actualExtent = value =>
 {
     Require(observed.Add(value));

     return measure(value);
 };
        lock (Field(runtime, EtsCompressed441Names.RuntimeGate)!)
        {
            object table = EtsFoundation405Access.Invoke(storage, EtsCompressed441Names.Resolve, id)!;
            object options = Field(storage, EtsCompressed441Names.Options)!;
            object?[] arguments = [table, null];
            Require((bool)options.GetType().GetMethod(EtsCompressed441Names.TryGet)!.Invoke(options, arguments)!);
            object census = storage.GetType().GetMethod(EtsCompressed441Names.Census, Members)!
                .Invoke(null, [table, arguments[1], actualExtent, CancellationToken.None])!;
            if (compressed is not null)
                Require(observed.Contains(compressed) && observed.Contains(Property(compressed, EtsCompressed441Names.Encoded)));
            Require(observed.Count(value => value.GetType().FullName == EtsCompressed441Names.BeamRecipe) == expectedBeamRecipes);
            Require(!observed.Any(value => EtsCompressed441Names.BorrowedFunctionOwners.Contains(value.GetType().FullName)));
            Require(!observed.Any(value => value is Term or ProcessRuntime or ProcessContext or ModuleRegistry or string));

            return (ulong)Property(census, EtsCompressed441Names.Bytes);
        }
    }

    internal static async Task LeaseAndCancellation()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsCompressed441Names.Kinds[0]);
        Term key = Term.I(EtsCompressed441Names.KeyField);
        await actor.InvokeAsync(async context =>
        {
            var resource = runtime.ReferenceResources.CreateBinary(new byte[] { 4, 4, 1 });

            return await Call(
                runtime,
                context,
                EtsCompressed441Names.Insert,
                table,
                Term.Tuple(resource, key)
            );
        });
        object lease = await actor.Invoke(context => EtsFoundation405Access.Invoke(
            storage,
            EtsCompressed441Names.Acquire,
            context,
            table,
            (long)Property(Slot(storage, table), EtsCompressed441Names.Id)
        )!);
        try
        {
            await actor.InvokeAsync(async context => await Call(
                runtime,
                context,
                EtsCompressed441Names.Delete,
                table
            ));
            Require((long)EtsFoundation405Access.Snapshot(storage)[3]! == 1);
            var copied = (TupleTerm)EtsFoundation405Access.Invoke(lease, EtsCompressed441Names.Materialize, CancellationToken.None)!;
            Require(copied.Items[EtsCompressed441Names.KeyField - 1].Equals(key));
            using var binary = runtime.ReferenceResources.AcquireBinary((ReferenceTerm)copied.Items[EtsCompressed441Names.ValueField - 1]);
            Require(binary!.Value.ToArray().SequenceEqual(new byte[] { 4, 4, 1 }));
        }
        finally
        {
            ((IDisposable)lease).Dispose();
        }
        EtsFoundation405Access.Empty(storage);
        object accounting = Field(storage, EtsCompressed441Names.Accounting)!;
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(EtsCompressed441Names.StoredType, true)!
            .GetMethod(EtsCompressed441Names.Capture, Members)!;
        bool canceled = false;
        try
        {
            _ = capture.Invoke(
                null,
                [accounting, runtime, Term.Tuple(Term.I(1), key), new CancellationToken(true), true, (long)EtsCompressed441Names.KeyField]
            );
        }
        catch (TargetInvocationException error) when (error.InnerException is OperationCanceledException)
        {
            canceled = true;
        }
        Require(canceled);
        EtsFoundation405Access.Empty(storage);
    }
}
