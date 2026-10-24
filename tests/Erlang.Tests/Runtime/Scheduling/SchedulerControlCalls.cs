namespace Erlang.Tests;

internal static class SchedulerControlCalls
{
    private static readonly Atom Binding = Term.A(SchedulerAffinityHostNames.Scheduler);
    private static readonly Atom Online = Term.A(SchedulerAffinityHostNames.Online);
    private static readonly Atom KillAtom = Term.A(SchedulerControlNames.Kill);

    internal static ValueTask<Term> Bind(ProcessContext context, int lane) => context.Runtime.Modules.Call(
        context,
        SchedulerAffinityHostNames.Erlang,
        SchedulerAffinityHostNames.Flag,
        Binding,
        Term.I(lane)
    );

    internal static ValueTask<Term> SetOnline(ProcessContext context, int count) => context.Runtime.Modules.Call(
        context,
        SchedulerAffinityHostNames.Erlang,
        SchedulerAffinityHostNames.SystemFlag,
        Online,
        Term.I(count)
    );

    internal static ValueTask<Term> Exit(ProcessContext sender, Pid target) => sender.Runtime.Modules.Call(
        sender,
        SchedulerAffinityHostNames.Erlang,
        SchedulerControlNames.Exit,
        target,
        KillAtom
    );

    internal static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal static TimeSpan Deadline => TimeSpan.FromSeconds(SchedulerAffinityHostNames.TimeoutSeconds);

    internal static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SchedulerAffinityHostNames.Failure);
    }

    internal static async Task Normal(ProcessHandle process)
    {
        Require((await process.Completion.WaitAsync(Deadline)).Equals(Term.A(SchedulerAffinityHostNames.Normal)));
    }

    internal static async Task Kill(ProcessRuntime runtime, Pid target)
    {
        var sender = new SchedulerControlActor(runtime, SchedulerAffinityHostNames.First, async context =>
        {
            await Exit(context, target);

            return Nil.Value;
        });
        await Normal(sender.Process);
    }
}
