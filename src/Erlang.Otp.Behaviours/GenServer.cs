

namespace Erlang.Otp;

public static class GenServer
{
    public static async ValueTask<ProcessHandle> Start(ProcessRuntime runtime, IGenServer callback, Term arguments, ProcessContext? parent = null, bool link = false)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var process = runtime.Spawn(async context =>
        {
            Term? state = null;
            Term reason = Term.A("normal");
            try
            {
                state = await callback.Init(context, arguments);
                ready.TrySetResult();
                while (true)
                {
                    await context.ReduceAsync();
                    var message = (await context.ReceiveAsync(t => t))!;
                    ServerResult result;
                    if (message is TupleTerm { Items.Count: 3 } call && call.Items[0].Equals(Term.A("$gen_call")) && call.Items[1] is TupleTerm { Items.Count: 2 } from && from.Items[0] is Pid pid && from.Items[1] is ReferenceTerm reference)
                    {
                        result = await callback.HandleCall(context, call.Items[2], new(pid, reference), state);
                        if (result.Reply is not null)
                            Reply(context, new(pid, reference), result.Reply);
                    }
                    else if (message is TupleTerm { Items.Count: 2 } cast && cast.Items[0].Equals(Term.A("$gen_cast")))
                        result = await callback.HandleCast(context, cast.Items[1], state);
                    else
                        result = await callback.HandleInfo(context, message, state);
                    state = result.State;
                    if (result.StopReason is not null)
                    {
                        reason = result.StopReason;
                        context.Exit(reason);
                    }
                }
            }
            catch (ErlangException ex) { reason = ex.Reason; ready.TrySetException(ex); throw; }
            catch (Exception ex) { ready.TrySetException(ex); throw; }
            finally { if (state is not null && context.ExitReason is null) await callback.Terminate(context, reason, state); }
        }, parent, link);
        await ready.Task;
        return process;
    }
    public static void Cast(ProcessContext context, Pid server, Term request) => context.Runtime.Send(server, Term.Tuple(Term.A("$gen_cast"), request));
    public static void Reply(ProcessContext context, ServerFrom from, Term reply) => context.Runtime.Send(from.Pid, Term.Tuple(from.Reference, reply));
    public static async ValueTask<Term> Call(ProcessContext context, Pid server, Term request, TimeSpan? timeout = null)
    {
        var reference = context.Runtime.Monitor(context, server);
        try
        {
            context.Runtime.Send(server, Term.Tuple(Term.A("$gen_call"), Term.Tuple(context.Self, reference), request));
            var response = await context.ReceiveAsync(t => t is TupleTerm tuple && ((tuple.Items.Count == 2 && tuple.Items[0].Equals(reference)) || (tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A("DOWN")) && tuple.Items[1].Equals(reference))) ? tuple : null, timeout ?? TimeSpan.FromSeconds(5));
            if (response is null)
                throw new ErlangException(Term.A("timeout"), "exit");
            if (response.Items.Count == 5)
                throw new ErlangException(response.Items[4], "exit");
            return response.Items[1];
        }
        finally { context.Runtime.Demonitor(context, reference, true); }
    }
}
