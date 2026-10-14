namespace Erlang;

internal sealed partial class RuntimePorts
{
    private RuntimePort[] haltingPorts = [];

    internal Task BeginHalt(bool flush)
    {
        lock (gate)
        {
            haltingPorts = ports.Values.ToArray();
            foreach (RuntimePort port in haltingPorts)
            {
                port.HaltOwnedLifetime = true;
                port.HaltChildExit = port.Process.HasChild ? port.Process.WaitForExitAsync(CancellationToken.None) : Task.CompletedTask;
            }
            Task[] prior = pumps.ToArray();
            Task close = flush
                ? Task.WhenAll(haltingPorts.Select(port => Task.Run(() => DrainHaltingPort(port))))
                : ForceHalt();

            return Task.WhenAll(close, Task.WhenAll(prior));
        }
    }

    internal Task ForceHalt()
    {
        lock (gate)
        {
            foreach (RuntimePort port in haltingPorts)
                Close(port, Term.A(ProcessExitReasons.Shutdown));

            return Task.WhenAll(haltingPorts.Select(port => port.Cleanup));
        }
    }

    private async Task DrainHaltingPort(RuntimePort port)
    {
        try
        {
            await port.Commands.ConfigureAwait(false);
            Task writes;
            Task? closed;
            lock (gate)
            {
                writes = port.Writes;
                closed = port.Closed ? port.Cleanup : null;
            }
            if (closed is not null)
            {
                await closed.ConfigureAwait(false);

                return;
            }
            await writes.ConfigureAwait(false);
            port.Input.Close();
            await port.HaltChildExit.ConfigureAwait(false);
        }
        finally
        {
            lock (gate)
                Close(port, Term.A(ProcessExitReasons.Shutdown));
        }
        await port.Cleanup.ConfigureAwait(false);
    }

    private async Task FinishOpening(TaskCompletionSource operation, Task cleanup)
    {
        try
        {
            await cleanup.ConfigureAwait(false);
            operation.TrySetResult();
        }
        catch (Exception exception)
        {
            operation.TrySetException(exception);
        }
    }
}
