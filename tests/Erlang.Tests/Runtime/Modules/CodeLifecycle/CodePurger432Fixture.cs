using Erlang;
namespace Erlang.Tests;

internal sealed class CodePurger432Fixture : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new(CodePurger432HostNames.Schedulers);
    internal RuntimeNodeFoundationHostActor Actor { get; private set; } = null!;
    internal ProcessHandle Purger { get; private set; } = null!;
    internal BeamDependencySelection Dependencies { get; } = new([CodePurger432HostNames.ErLang, CodePurger432HostNames.Internal, CodePurger432HostNames.Module, CodePurger432HostNames.Subject, CodePurgerOriginal432Names.Module, CodePurgerOriginal432Names.File]);

    internal static async Task<CodePurger432Fixture> Create()
    {
        var fixture = new CodePurger432Fixture();
        try
        {
            await fixture.Runtime.InitializeModulesAsync(registry =>
            {
                var facet = registry.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.FacetBeam), fixture.Dependencies);
                var purger = registry.PrepareBeam(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.PurgerBeam), fixture.Dependencies);
                CodePurger432HostAccess.Require(registry.PublishPreparedBeam([facet, purger]).Status == BeamPublicationStatus.Published);
            });
            fixture.Actor = new(fixture.Runtime);
            fixture.Purger = await fixture.Actor.Invoke(context =>
            {
                object?[] args = [context, CodePurger432HostNames.Module, CodePurger432HostNames.Start, Array.Empty<Term>(), null];
                bool started = (bool)typeof(ProcessRuntime).GetMethod(CodePurger432HostNames.Spawn, CodePurger432HostNames.Flags)!.Invoke(fixture.Runtime, args)!;
                CodePurger432HostAccess.Require(started);

                return (ProcessHandle)args[4]!;
            });
            long start = Environment.TickCount64;
            while (!fixture.Runtime.WhereIs(CodePurger432HostNames.Module).Equals(fixture.Purger.Pid)
                && Environment.TickCount64 - start < CodePurger432HostNames.Deadline.TotalMilliseconds) await Task.Delay(1);
            CodePurger432HostAccess.Require(fixture.Runtime.WhereIs(CodePurger432HostNames.Module).Equals(fixture.Purger.Pid));

            return fixture;
        }
        catch (Exception error)
        {
            try
            {
                await fixture.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Close actual runtime/session owners first; a blocked source admission
        // must not keep the host command reader from reaching its channel end.
        Exception? failure = null;
        try
        {
            await Runtime.DisposeAsync();
        }
        catch (Exception error)
        {
            failure = error;
        }
        if (Actor is not null)
            try
            {
                await Actor.DisposeAsync();
            }
            catch (Exception error)
            {
                if (failure is not null) throw new AggregateException(failure, error);
                throw;
            }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
