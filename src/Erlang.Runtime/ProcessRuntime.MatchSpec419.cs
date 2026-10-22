namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireMatchSpecActor419(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    internal ResourceReferenceTerm PublishMatchSpec419(ProcessContext context, EtsCompiledProgram419 program)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();

            return ReferenceResources.CreateMatchSpec419(program);
        }
    }
}
