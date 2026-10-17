namespace Erlang.Tests;

public static class BacktraceDepthHostChecks
{
    public static async Task IndependentRuntimes()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        var actor = first.Spawn(async context =>
        {
            Require((await Set(context, BacktraceDepthHostParameters.FirstDepth)).Equals(Term.I(BacktraceDepthHostParameters.DefaultDepth)));

            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await actor.Completion.WaitAsync(BacktraceDepthHostParameters.Deadline)).Equals(Term.A(BacktraceDepthHostNames.Normal)));
        var other = second.Spawn(async context =>
        {
            Require((await Set(context, BacktraceDepthHostParameters.SecondDepth)).Equals(Term.I(BacktraceDepthHostParameters.DefaultDepth)));

            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await other.Completion.WaitAsync(BacktraceDepthHostParameters.Deadline)).Equals(Term.A(BacktraceDepthHostNames.Normal)));
        var observed = first.Spawn(async context =>
        {
            Require((await Set(context, BacktraceDepthHostParameters.DefaultDepth)).Equals(Term.I(BacktraceDepthHostParameters.FirstDepth)));

            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await observed.Completion.WaitAsync(BacktraceDepthHostParameters.Deadline)).Equals(Term.A(BacktraceDepthHostNames.Normal)));
    }

    private static ValueTask<Term> Set(ProcessContext context, int depth) => context.Runtime.Modules.Call(
        context,
        BacktraceDepthHostNames.Module,
        BacktraceDepthHostNames.Function,
        Term.A(BacktraceDepthHostNames.Flag),
        Term.I(depth)
    );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BacktraceDepthHostNames.Failure);
    }
}
