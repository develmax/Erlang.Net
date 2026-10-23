namespace Erlang;

public sealed partial class ModuleRegistry
{
    private ModuleCodeModificationPermission? codeModificationPermission;

    private ModuleCodeModificationPermission CodeModification => Volatile.Read(ref codeModificationPermission) ?? CreateCodeModification();

    private ModuleCodeModificationPermission CreateCodeModification() => LazyInitializer.EnsureInitialized(
        ref codeModificationPermission,
        () => new ModuleCodeModificationPermission(gate)
    );

    internal async ValueTask<T> ModifyCodeAsync<T>(Func<T> operation, CancellationToken cancellation = default)
    {
        using var ticket = await CodeModification.AcquireAsync(cancellation).ConfigureAwait(false);

        return ticket.Run(operation);
    }

    internal async ValueTask<T> ModifyCodeAsync<T>(ProcessContext context, Func<T> operation)
    {
        using var ticket = await AcquireCodeModificationAsync(context).ConfigureAwait(false);

        return ticket.Run(operation);
    }

    private async ValueTask<ModuleCodeModificationTicket> AcquireCodeModificationAsync(ProcessContext context)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(context.Cancellation);
        Task<ModuleCodeModificationTicket> pending = CodeModification.AcquireAsync(cancellation.Token).AsTask();
        ModuleCodeModificationTicket ticket;
        try
        {
            await context.Mailbox.WaitForControlCompletion(pending, context.Cancellation);
            ticket = await pending.ConfigureAwait(false);
        }
        catch
        {
            cancellation.Cancel();
            try
            {
                using var abandoned = await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            throw;
        }
        try
        {
            context.Cancellation.ThrowIfCancellationRequested();

            return ticket;
        }
        catch
        {
            ticket.Dispose();
            throw;
        }
    }
}
