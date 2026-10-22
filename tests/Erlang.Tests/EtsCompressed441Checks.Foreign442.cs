using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static partial class EtsCompressed441Checks
{
    internal static async Task ForeignOwners442()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        Install(runtime);
        PreparedBeamImage image = BeamStoredFunction406Access.Publish(foreign);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        ProcessContext context = await other.Ready;
        FunctionTerm function = await other.InvokeAsync(async caller => (FunctionTerm)await foreign.Modules.Call(
            caller,
            image.Name,
            BeamClosureHostNames.Make,
            Term.I(BeamClosureHostNames.Capture)
        ));
        ResourceReferenceTerm resource = foreign.ReferenceResources.CreateBinary(BeamStoredFunction406Names.Bytes);
        object storage = EtsFoundation405Access.Get(runtime);
        Term table = await Create(runtime, actor, EtsCompressed441Names.Kinds[0]);
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(EtsCompressed441Names.StoredType, true)!
            .GetMethod(EtsCompressed441Names.Capture, Members)!;
        foreach (Term invalid in new Term[] { context.Self, resource, function })
        {
            await actor.Invoke(caller =>
            {
                var before = EtsFoundation405Access.Snapshot(storage);
                bool refused = false;
                try
                {
                    object? captured = capture.Invoke(
                        null,
                        [Field(storage, EtsCompressed441Names.Accounting), runtime,
                        Term.Tuple(Term.Tuple(caller.Self, invalid), Term.I(EtsCompressed441Names.KeyField)),
                        CancellationToken.None, true, (long)EtsCompressed441Names.KeyField]
                    );
                    if (captured is not null) EtsFoundation405Access.Invoke(captured, EtsCodecCancellation442Names.Release);
                }
                catch (TargetInvocationException error) when (error.InnerException is ArgumentException or NotSupportedException)
                {
                    refused = true;
                }
                Require(refused && before.Equals(EtsFoundation405Access.Snapshot(storage)));

                return true;
            });
        }
        await actor.InvokeAsync(async caller => await Call(
            runtime,
            caller,
            EtsCompressed441Names.Delete,
            table
        ));
        EtsFoundation405Access.Empty(storage);
    }
}
