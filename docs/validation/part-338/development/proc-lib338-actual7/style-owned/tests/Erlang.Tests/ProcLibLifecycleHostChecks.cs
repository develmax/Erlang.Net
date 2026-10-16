using System.Text;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibLifecycleHostChecks
{
    public static async Task IsolatedConfiguration()
    {
        await using var a = CreateRuntime();
        await using var b = CreateRuntime();
        await using var first = new RuntimeNodeFoundationHostActor(a);
        await using var second = new RuntimeNodeFoundationHostActor(b);
        Require(
            await Call(
                first,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.SetPrimary,
                new MapTerm([new(Term.A(ProcLibLifecycleHostNames.Level), Term.A(ProcLibLifecycleHostNames.Debug))])
            ),
            ProcLibLifecycleHostAtoms.Ok
        );
        Require(
            await Call(
                first,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.Allow,
                Term.A(ProcLibLifecycleHostNames.Debug),
                ProcLibLifecycleHostAtoms.Module
            ),
            ProcLibLifecycleHostAtoms.True
        );
        Require(
            await Call(
                second,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.Allow,
                Term.A(ProcLibLifecycleHostNames.Debug),
                ProcLibLifecycleHostAtoms.Module
            ),
            ProcLibLifecycleHostAtoms.False
        );
        Require(
            await Call(
                first,
                ProcLibLifecycleHostNames.Application,
                ProcLibLifecycleHostNames.SetEnv,
                Term.A(ProcLibLifecycleHostNames.Kernel),
                Term.A(ProcLibLifecycleHostNames.Key),
                Term.I(ProcLibLifecycleHostNames.IsolationValue)
            ),
            ProcLibLifecycleHostAtoms.Ok
        );
        Require(
            await Call(
                first,
                ProcLibLifecycleHostNames.Application,
                ProcLibLifecycleHostNames.GetEnv,
                Term.A(ProcLibLifecycleHostNames.Kernel),
                Term.A(ProcLibLifecycleHostNames.Key),
                Term.A(ProcLibLifecycleHostNames.Fallback)
            ),
            Term.I(ProcLibLifecycleHostNames.IsolationValue)
        );
        Require(
            await Call(
                second,
                ProcLibLifecycleHostNames.Application,
                ProcLibLifecycleHostNames.GetEnv,
                Term.A(ProcLibLifecycleHostNames.Kernel),
                Term.A(ProcLibLifecycleHostNames.Key),
                Term.A(ProcLibLifecycleHostNames.Fallback)
            ),
            Term.A(ProcLibLifecycleHostNames.Fallback)
        );
    }

    public static Task AwaitHandler() => HandlerBoundary(false);

    public static Task CancelHandler() => HandlerBoundary(true);

    private static async Task HandlerBoundary(bool cancel)
    {
        await using var runtime = CreateRuntime();
        await using var controller = new RuntimeNodeFoundationHostActor(runtime);
        Require(
            await Call(
                controller,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.RemoveHandler,
                ProcLibLifecycleHostAtoms.Default
            ),
            ProcLibLifecycleHostAtoms.Ok
        );
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool returned = false;
        runtime.Modules.Register(
            ProcLibLifecycleHostNames.Module,
            ProcLibLifecycleHostNames.Log,
            ProcLibLifecycleHostNames.HandlerArity,
            async (context, args) =>
            {
                if (args[0] is not MapTerm)
                    throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
                entered.TrySetResult();
                await released.Task.WaitAsync(context.Cancellation);

                return ProcLibLifecycleHostAtoms.Ok;
            }
        );
        Require(
            await Call(
                controller,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.AddHandler,
                ProcLibLifecycleHostAtoms.Handler,
                ProcLibLifecycleHostAtoms.Module,
                ProcLibLifecycleHostAtoms.Empty
            ),
            ProcLibLifecycleHostAtoms.Ok
        );
        var caller = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await Emit(context, ProcLibLifecycleHostAtoms.Empty);
                returned = true;

                return result;
            }
            finally
            {
                unwound.TrySetResult();
            }
        });
        try
        {
            await entered.Task.WaitAsync(ProcLibLifecycleHostNames.Deadline);
            if (caller.Completion.IsCompleted || returned)
                throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
            if (cancel)
                await controller.Invoke(context =>
                {
                    runtime.Exit(context, caller.Pid, Term.A(ProcLibLifecycleHostNames.Kill));

                    return true;
                });
            else
                released.TrySetResult();
            Term reason = await caller.Completion.WaitAsync(ProcLibLifecycleHostNames.Deadline);
            await unwound.Task.WaitAsync(ProcLibLifecycleHostNames.Deadline);
            Require(reason, Term.A(cancel ? ProcLibLifecycleHostNames.Killed : ProcLibLifecycleHostNames.Normal));
            if (returned == cancel)
                throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
            Term handler = await Call(
                controller,
                ProcLibLifecycleHostNames.Logger,
                ProcLibLifecycleHostNames.GetHandler,
                ProcLibLifecycleHostAtoms.Handler
            );
            if (handler is not TupleTerm { Items: [Atom { Name: ProcLibLifecycleHostNames.Ok }, MapTerm] })
                throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
        }
        finally
        {
            released.TrySetResult();
        }
    }

    public static async Task ActualStandardOutput()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        OtpModules.Register(runtime.Modules);
        await using var caller = new RuntimeNodeFoundationHostActor(runtime);
        bool invoked = false;
        var callback = new FunctionTerm(
            ProcLibLifecycleHostNames.HandlerArity,
            (_, args) =>
        {
            if (!args[0].Equals(ProcLibLifecycleHostAtoms.Message) || args[1] is not MapTerm options ||
                !options.Get(Term.A(ProcLibLifecycleHostNames.SingleLine)).Equals(ProcLibLifecycleHostAtoms.True))
                throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
            invoked = true;

            return ValueTask.FromResult<Term>(Term.String(ProcLibLifecycleHostNames.Rendered));
        }
        );
        Require(
            await caller.InvokeAsync(async context => await Emit(context,
            new MapTerm([new(Term.A(ProcLibLifecycleHostNames.ReportCallback), callback)]))),
            ProcLibLifecycleHostAtoms.Ok
        );
        string actual = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            Role = nameof(ActualStandardOutput),
            CallbackInvoked = invoked,
            StandardOutputHex = Convert.ToHexString(output.ToArray()),
            StandardErrorHex = Convert.ToHexString(error.ToArray())
        }));
        if (!invoked || actual != ProcLibLifecycleHostNames.Rendered + ProcLibLifecycleHostNames.Newline || error.Length != 0)
            throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
    }

    private static ProcessRuntime CreateRuntime()
    {
        var runtime = new ProcessRuntime();
        OtpModules.Register(runtime.Modules);

        return runtime;
    }

    private static ValueTask<Term> Emit(ProcessContext context, Term metadata)
        => context.Runtime.Modules.Call(
            context,
            ProcLibLifecycleHostNames.Logger,
            ProcLibLifecycleHostNames.MacroLog,
            ProcLibLifecycleHostAtoms.Empty,
            ProcLibLifecycleHostAtoms.Error,
            ProcLibLifecycleHostAtoms.Message,
            metadata
        );

    private static Task<Term> Call(
        RuntimeNodeFoundationHostActor actor,
        string module,
        string function,
        params Term[] args
    )
        => actor.InvokeAsync(async context => await context.Runtime.Modules.Call(
            context,
            module,
            function,
            args
        ));

    private static void Require(Term actual, Term expected)
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException(ProcLibLifecycleHostNames.Mismatch);
    }
}
