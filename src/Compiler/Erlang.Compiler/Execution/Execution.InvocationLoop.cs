namespace Erlang.Compiler;

public static partial class Execution
{
    private static async ValueTask<Term> InvokeTargetAsync(
        SourceInvocationTarget target,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    )
    {
        OwnedTermInvocation506? trampoline = null;
        var module = target.Module;
        var image = target.Image;
        var lease = target.Lease;
        try
        {
            trampoline = context.EnterOwnedTrampoline506(arguments);
            arguments = trampoline?.Arguments ?? arguments;
            if (trampoline is not null)
            {
                var initial = SourceContinuationPayload506.Target(target, arguments, trampoline.HoldControl506);
                target = initial.Target;
                arguments = initial.Arguments;
            }
            while (true)
            {
                if (lease is null && image is not null)
                    lease = await context.Runtime.Modules.AcquireSourceCode(context, image);
                try
                {
                    using var flow = module is null ? null : lease?.Attach(module);
                    Term result = target.Expression is { } expression
                        ? await InvokeAnonymousFrame(
                            module,
                            target.Identity!,
                            expression,
                            target.Environment!,
                            context,
                            image,
                            arguments
                        )
                        : await InvokeFrame(
                            module!,
                            target.Function!,
                            context,
                            arguments,
                            image
                        );

                    return trampoline?.Complete(result) ?? result;
                }
                catch (TailCall next) when (module is not null)
                {
                    target = new(
                        module,
                        next.Function,
                        null,
                        null,
                        null,
                        next.Image
                    );
                    arguments = next.Arguments;
                    image = next.Image;
                    if (trampoline is not null)
                        await OwnedTrampolineCheckpoint506.Reach(context, nameof(TailCall));
                }
                catch (SourceTailCall next)
                {
                    lease?.Dispose();
                    lease = null;
                    module = next.Module;
                    image = next.Image;
                    target = new(
                        module,
                        next.Function,
                        null,
                        null,
                        null,
                        image
                    );
                    arguments = next.Arguments;
                    if (trampoline is not null)
                        await OwnedTrampolineCheckpoint506.Reach(context, nameof(SourceTailCall));
                }
                catch (RuntimeTailCall next)
                {
                    lease?.Dispose();
                    lease = null;
                    if (trampoline is not null)
                        await OwnedTrampolineCheckpoint506.Reach(context, nameof(RuntimeTailCall));
                    Term result = await CallRuntime(
                        context,
                        next.Module,
                        next.Function,
                        next.Arguments
                    );

                    return trampoline?.Complete(result) ?? result;
                }
                catch (FunctionTailCall next)
                {
                    if (trampoline is not null)
                        await OwnedTrampolineCheckpoint506.Reach(context, nameof(FunctionTailCall));
                    if (!SourceFunctionTailDispatch.IsSource(context, next.Function))
                    {
                        lease?.Dispose();
                        lease = null;
                        Term result = await next.Function.Invoke(context, next.Arguments);

                        return trampoline?.Complete(result) ?? result;
                    }
                    using var transferScope = SourceFunctionTailDispatch.Enter(context);
                    try
                    {
                        Term result = await next.Function.Invoke(context, next.Arguments);

                        return trampoline?.Complete(result) ?? result;
                    }
                    catch (SourceFunctionTransfer transfer) when (ReferenceEquals(transfer.Scope, transferScope))
                    {
                        (SourceInvocationTarget Target, IReadOnlyList<Term> Arguments) rooted;
                        try
                        {
                            if (trampoline is null)
                                rooted = (transfer.Target, next.Arguments);
                            else
                            {
                                var published = SourceContinuationPayload506.Target(transfer.Target, next.Arguments, trampoline.HoldControl506);
                                rooted = (published.Target, published.Arguments);
                            }
                        }
                        catch
                        {
                            transfer.Target.Lease?.Dispose();
                            throw;
                        }
                        lease?.Dispose();
                        target = rooted.Target;
                        arguments = rooted.Arguments;
                        module = target.Module;
                        image = target.Image;
                        lease = target.Lease;
                        if (trampoline is not null)
                            await OwnedTrampolineCheckpoint506.Reach(context, nameof(SourceFunctionTransfer));
                    }
                }
            }
        }
        catch (ErlangException error) when (trampoline is not null)
        {
            throw trampoline.Transfer(error);
        }
        catch (Exception control) when (trampoline is not null && control is IOwnedControlTransfer506)
        {
            throw trampoline.TransferControl506(control);
        }
        finally
        {
            try
            {
                lease?.Dispose();
            }
            finally
            {
                trampoline?.Dispose();
            }
        }
    }
}
