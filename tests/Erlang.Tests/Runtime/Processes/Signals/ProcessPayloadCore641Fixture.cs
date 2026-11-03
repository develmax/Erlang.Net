using System.Runtime.ExceptionServices;
using static Erlang.Tests.ProcessPayloadCore641Access;
using static Erlang.Tests.ProcessPayloadCore641Names;

namespace Erlang.Tests;

internal sealed class ProcessPayloadCore641Fixture
{
    private readonly List<RuntimeNodeFoundationHostActor> actors = new();
    private readonly List<object> payloads = new();
    internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
    internal Exception? ExpectedFailure { get; set; }

    internal async Task<(RuntimeNodeFoundationHostActor Actor, ProcessContext Context)> Actor()
    {
        var actor = new RuntimeNodeFoundationHostActor(Runtime);
        actors.Add(actor);

        return (actor, await actor.Ready.WaitAsync(Deadline));
    }

    internal object Hold(object payload)
    {
        payloads.Add(payload);

        return payload;
    }

    internal async Task Finish(List<Exception> failures)
    {
        foreach (object payload in payloads)
            try
            {
                Call(payload, Close);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        foreach (var actor in actors)
            try
            {
                await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error) when (ExpectedFailure is not null && OnlyExpected(error, ExpectedFailure)) { }
            catch (Exception error)
            {
                failures.Add(error);
            }
        try
        {
            await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        catch (Exception error) when (ExpectedFailure is not null && OnlyExpected(error, ExpectedFailure)) { }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }
}
