using Erlang;
namespace Erlang.Tests;

internal static class InternalPortLoader428HostChecks
{
    internal static async Task StatusAndOwner()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        var call = InternalPortLoader428HostAccess.Body(runtime, InternalPortLoader428HostNames.Call, 3);
        var control = InternalPortLoader428HostAccess.Body(runtime, InternalPortLoader428HostNames.Control, 3);
        var prepare = InternalPortLoader428HostAccess.Body(runtime, InternalPortLoader428HostNames.Prepare, 2);
        await actor.InvokeAsync(async context =>
        {
            foreach (var body in new[] { call, control })
                InternalPortLoader428HostAccess.Require((await body(
                    context,
                    new Term[] { Term.A(InternalPortLoader428HostNames.Missing), Term.I(0), Term.A(InternalPortLoader428HostNames.Data) }
                )).Equals(Term.A(InternalPortLoader428HostNames.BadArgument)));
            bool bad = false;
            try
            {
                await prepare(context, new Term[] { Term.I(17), new BitString(Array.Empty<byte>()) });
            }
            catch (ErlangException error)
            {
                bad = error.Reason.Equals(Term.A(InternalPortLoader428HostNames.BadArgument));
                Term? frame = Cons.Items(error.StackTraceTerm).FirstOrDefault();
                InternalPortLoader428HostAccess.Require(frame is TupleTerm { Items: [Atom { Name: InternalPortLoader428HostNames.Module }, Atom { Name: InternalPortLoader428HostNames.Prepare }, _, _] });
            }
            InternalPortLoader428HostAccess.Require(bad);

            return true;
        });
        await other.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await call(context, new Term[] { Term.A(InternalPortLoader428HostNames.Missing), Term.I(0), Nil.Value });
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            InternalPortLoader428HostAccess.Require(refused);

            return true;
        });
    }

    internal static async Task PreparedImage()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var body = InternalPortLoader428HostAccess.Body(runtime, InternalPortLoader428HostNames.Prepare, 2);
        await actor.InvokeAsync(async context =>
        {
            Term name = Term.A(InternalPortLoader428HostNames.Leaf);
            Term expected = Term.Tuple(Term.A(InternalPortLoader428HostNames.Error), Term.A(InternalPortLoader428HostNames.BadFile));
            InternalPortLoader428HostAccess.Require((await body(
                context,
                new Term[] { name, new BitString(InternalPortLoader428HostAccess.Resource(InternalPortLoader428HostNames.LeafGzip)) }
            )).Equals(expected));
            Term prepared = await body(
                context,
                new Term[] { name, new BitString(InternalPortLoader428HostAccess.Resource(InternalPortLoader428HostNames.LeafBeam)) }
            );
            InternalPortLoader428HostAccess.Require(prepared is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.PreparedBeam });
            using var lease = runtime.ReferenceResources.AcquirePrepared((ReferenceTerm)prepared);
            InternalPortLoader428HostAccess.Require(lease is not null);
            PreparedBeamImage image = lease!.Value;
            InternalPortLoader428HostAccess.Require((await runtime.Modules.Call(
                context,
                InternalPortLoader428HostNames.Erlang,
                InternalPortLoader428HostNames.HasOnLoad,
                prepared
            )).Equals(Term.A(InternalPortLoader428HostNames.False)));
            InternalPortLoader428HostAccess.Require((await runtime.Modules.Call(
                context,
                InternalPortLoader428HostNames.Erlang,
                InternalPortLoader428HostNames.Finish,
                Term.List(prepared)
            )).Equals(Term.A(InternalPortLoader428HostNames.Ok)));
            InternalPortLoader428HostAccess.Require(ReferenceEquals(runtime.Modules.SourceOwner(InternalPortLoader428HostNames.Leaf, InternalPortLoader428HostNames.Answer, 0), image));
            InternalPortLoader428HostAccess.Require((await runtime.Modules.Call(context, InternalPortLoader428HostNames.Leaf, InternalPortLoader428HostNames.Answer)).Equals(Term.I(42)));

            return true;
        });
    }
}
