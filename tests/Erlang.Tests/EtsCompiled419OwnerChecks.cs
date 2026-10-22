namespace Erlang.Tests;

internal static class EtsCompiled419OwnerChecks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            await EtsCompiled419Access.Initialize(runtime);
            await EtsCompiled419Access.Initialize(foreign);
            actor = new(runtime);
            await using var other = new RuntimeNodeFoundationHostActor(foreign);
            Term program = await actor.InvokeAsync(async context => await EtsCompiled419Access.Call(context,
                EtsCompiled419Names.Compile, EtsCompiled419Access.IdentitySpec));
            ErlangFunction ownerBody = EtsCompiled419Access.Body(runtime, EtsCompiled419Names.IsCompiled, 1);
            ErlangFunction otherBody = EtsCompiled419Access.Body(foreign, EtsCompiled419Names.IsCompiled, 1);
            await other.InvokeAsync(async context =>
            {
                EtsCompiled419Access.Require((await otherBody(context, [program])).Equals(Term.A(EtsCompiled419Names.False)));
                bool refused = false;
                try
                {
                    _ = await ownerBody(context, [program]);
                }
                catch (ArgumentException)
                {
                    refused = true;
                }
                EtsCompiled419Access.Require(refused);

                return true;
            });
            ProcessContext saved = await actor.Ready;
            await actor.InvokeAsync(async context =>
            {
                int before = runtime.ReferenceResources.LiveResources;
                ((CancellationTokenSource)typeof(ProcessContext).GetProperty(PersistentArena416HostNames.Stop,
                    PersistentArena416HostNames.Members)!.GetValue(context)!).Cancel();
                bool refused = false;
                try
                {
                    _ = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, EtsCompiled419Access.IdentitySpec);
                }
                catch (OperationCanceledException)
                {
                    refused = true;
                }
                EtsCompiled419Access.Require(refused && runtime.ReferenceResources.LiveResources == before);

                return true;
            });
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                _ = await ownerBody(saved, [program]);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            EtsCompiled419Access.Require(closed && runtime.ReferenceResources.LiveResources == 0);
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            finally
            {
                await runtime.DisposeAsync();
            }
        }
    }
}
