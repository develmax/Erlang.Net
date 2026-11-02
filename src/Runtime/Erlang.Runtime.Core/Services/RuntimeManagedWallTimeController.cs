using System.Numerics;
using System.Threading.Channels;

namespace Erlang;

internal sealed class RuntimeManagedWallTimeController
{
    private readonly ProcessRuntime runtime;
    private readonly Channel<RuntimeWallTimeCommand> commands = Channel.CreateUnbounded<RuntimeWallTimeCommand>();
    private readonly Dictionary<Pid, BigInteger> references = new();
    private readonly ObjectDisposedException closed = new(RuntimeSchedulerWallTimeNames.Closed);
    private Exception? stopped;
    internal ProcessHandle Process { get; }

    internal RuntimeManagedWallTimeController(ProcessRuntime runtime)
    {
        this.runtime = runtime;
        Process = runtime.Spawn(Run);
    }

    internal RuntimeWallTimeFlagCommand Flag(Pid caller, bool enabled)
    {
        var command = new RuntimeWallTimeFlagCommand(caller, enabled);
        if (!commands.Writer.TryWrite(command)) throw Volatile.Read(ref stopped) ?? closed;

        return command;
    }

    internal Task Exit(Pid caller)
    {
        var command = new RuntimeWallTimeExitCommand(caller);
        if (!commands.Writer.TryWrite(command)) return Task.FromException(Volatile.Read(ref stopped) ?? closed);

        return command.Settled.Task;
    }

    private async ValueTask<Term> Run(ProcessContext context)
    {
        Exception? failure = null;
        try
        {
            await foreach (RuntimeWallTimeCommand command in commands.Reader.ReadAllAsync(context.Cancellation))
            {
                try
                {
                    await Apply(context, command);
                    command.Settled.TrySetResult();
                }
                catch (Exception error)
                {
                    // An explicit receipt distinguishes unstarted abort from
                    // published work. Accepted callback failures remain failures.
                    if (command.Acknowledgments is null && runtime.IsWallTimeClosing
                        && (error is OperationCanceledException or ObjectDisposedException))
                    {
                        command.Abandon(error);
                        break;
                    }
                    command.Fail(error);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (context.Cancellation.IsCancellationRequested)
        {
            // No already-started Apply is abandoned: it awaits its real callbacks.
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            Volatile.Write(ref stopped, failure ?? closed);
            commands.Writer.TryComplete(failure ?? closed);
            while (commands.Reader.TryRead(out RuntimeWallTimeCommand? command))
            {
                if (failure is not null) command.Fail(failure);
                else command.Abandon(closed);
            }
        }

        return Nil.Value;
    }

    private async ValueTask Apply(ProcessContext context, RuntimeWallTimeCommand command)
    {
        bool previous = references.Count != 0;
        references.TryGetValue(command.Caller, out BigInteger count);
        switch (command)
        {
            case RuntimeWallTimeFlagCommand { Enabled: true } start:
                references.EnsureCapacity(references.Count + 1);
                if (!previous) await runtime.SetManagedNativeWallTime(context, true, command);
                references[command.Caller] = count + BigInteger.One;
                start.Result.TrySetResult(previous);
                break;
            case RuntimeWallTimeFlagCommand stop:
                if (count.IsOne)
                {
                    if (references.Count == 1) await runtime.SetManagedNativeWallTime(context, false, command);
                    references.Remove(command.Caller);
                }
                else if (!count.IsZero) references[command.Caller] = count - BigInteger.One;
                stop.Result.TrySetResult(previous);
                break;
            case RuntimeWallTimeExitCommand:
                if (!count.IsZero)
                {
                    if (references.Count == 1) await runtime.SetManagedNativeWallTime(context, false, command);
                    references.Remove(command.Caller);
                }
                break;
        }
    }
}
