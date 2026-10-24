using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamApplyHostChecks
{
    internal static async Task Calls()
    {
        await using var runtime = new ProcessRuntime();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Modules.Register(
            BeamApplyHostNames.Module,
            BeamApplyHostNames.Echo,
            1,
            async (context, arguments) =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);

            return arguments[0];
        }
        );
        BeamModule zero = BeamModule.Prepare(Bytes(0));
        BeamModule one = BeamModule.Prepare(Bytes(1), new BeamDependencySelection([BeamApplyHostNames.Module]));
        Require(zero.Supports(BeamOpcodeEdgeNames.Run, 2) && one.Supports(BeamOpcodeEdgeNames.Run, 3));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term actual = await zero.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                Term.A(ErlangModuleNames.Module),
                Term.A(ErlangModuleNames.Self)
            );
            Require(actual.Equals(Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), context.Self)));

            return true;
        });
        Term argument = Term.Tuple(Term.I(42));
        Task<Term> call = actor.InvokeAsync(context => one.Invoke(
            context,
            BeamOpcodeEdgeNames.Run,
            argument,
            Term.A(BeamApplyHostNames.Module),
            Term.A(BeamApplyHostNames.Echo)
        ).AsTask());
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Require(!call.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }
        Require((await call).Equals(Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), argument)));
    }

    internal static async Task Errors()
    {
        await using var runtime = new ProcessRuntime();
        BeamModule module = BeamModule.Prepare(Bytes(0));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (Term[] arguments in new[] { new Term[] { Term.I(7), Term.A(ErlangModuleNames.Self) }, new Term[] { Term.A(ErlangModuleNames.Module), Term.I(7) } })
        {
            bool caught = false;
            try
            {
                await actor.InvokeAsync(context => module.Invoke(context, BeamOpcodeEdgeNames.Run, arguments).AsTask());
            }
            catch (ErlangException error)
            {
                Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                TupleTerm frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                Require(frame.Items[0].Equals(Term.A(ErlangModuleNames.Module))
                    && frame.Items[1].Equals(Term.A(ErlangBasicBifNames.Apply))
                    && frame.Items[2].Equals(Term.List(arguments[0], arguments[1], Nil.Value)));
                caught = true;
            }
            Require(caught);
        }
        BeamModule missing = BeamModule.Prepare(Bytes(0), new BeamDependencySelection([BeamApplyHostNames.Module]));
        bool undefined = false;
        try
        {
            await actor.InvokeAsync(context => missing.Invoke(
                context,
                BeamOpcodeEdgeNames.Run,
                Term.A(BeamApplyHostNames.Module),
                Term.A(BeamApplyHostNames.Echo)
            ).AsTask());
        }
        catch (ErlangException error)
        {
            TupleTerm frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction))
                && frame.Items[0].Equals(Term.A(BeamApplyHostNames.Module))
                && frame.Items[1].Equals(Term.A(BeamApplyHostNames.Echo))
                && frame.Items[2] is Nil && frame.Items[3] is Nil);
            undefined = true;
        }
        Require(undefined);
        foreach (byte[] code in new[]
        {
            Bytes(1, entryArity: 1),
            Bytes(0, stale: true),
            Image([.. Start(
                1,
                2,
                2,
                2
            ), BeamApplyHostNames.Opcode, U(0), 19], labels: 3, arity: 2)
        })
        {
            bool refused = false;
            try
            {
                _ = runtime.Modules.PrepareBeam(code);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                refused = true;
            }
            Require(refused);
        }
    }

    private static byte[] Bytes(int arity, int? entryArity = null, bool stale = false)
    {
        int parameters = entryArity ?? arity + 2;
        byte[] code = [.. Start(
            1,
            2,
            2,
            parameters
        ), 12, U(0), U(parameters),
            BeamApplyHostNames.Opcode, U(arity),
            .. (stale ? new byte[] { 64, X(1), X(0) } : Array.Empty<byte>()),
            164, X(0), 0x17, U(2), A(7), X(0), 18, U(0), 19];

        return Image(code, labels: 3, arity: parameters);
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamApplyHostNames.Failure);
    }
}
