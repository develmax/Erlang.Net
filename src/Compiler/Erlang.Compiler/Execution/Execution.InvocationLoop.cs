namespace Erlang.Compiler;

public static partial class Execution
{
    private static async ValueTask<Term> InvokeTargetAsync(
        SourceInvocationTarget target,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    )
    {
        var module = target.Module;
        var image = target.Image;
        var lease = target.Lease;
        try
        {
            while (true)
            {
                if (lease is null && image is not null)
                {
                    lease = await context.Runtime.Modules.AcquireSourceCode(context, image);
                }
                try
                {
                    using var flow = module is null ? null : lease?.Attach(module);

                    return target.Expression is { } expression
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
                }
                catch (RuntimeTailCall next)
                {
                    lease?.Dispose();
                    lease = null;

                    return await CallRuntime(
                        context,
                        next.Module,
                        next.Function,
                        next.Arguments
                    );
                }
                catch (FunctionTailCall next)
                {
                    if (!SourceFunctionTailDispatch.IsSource(context, next.Function))
                    {
                        lease?.Dispose();
                        lease = null;

                        return await next.Function.Invoke(context, next.Arguments);
                    }
                    using var transferScope = SourceFunctionTailDispatch.Enter(context);
                    try
                    {
                        return await next.Function.Invoke(context, next.Arguments);
                    }
                    catch (SourceFunctionTransfer transfer) when (ReferenceEquals(transfer.Scope, transferScope))
                    {
                        lease?.Dispose();
                        target = transfer.Target;
                        module = target.Module;
                        image = target.Image;
                        lease = target.Lease;
                        arguments = next.Arguments;
                    }
                }
            }
        }
        finally
        {
            lease?.Dispose();
        }
    }
}
