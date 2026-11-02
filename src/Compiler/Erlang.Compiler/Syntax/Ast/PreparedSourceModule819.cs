namespace Erlang.Compiler;

public sealed class PreparedSourceModule819 : IDisposable, IAsyncDisposable
{
    private readonly ModuleDefinition template;
    private readonly ModuleRegistry registry;
    private readonly PreparedSourceLiteralView819 literals;

    internal PreparedSourceModule819(ModuleDefinition template, ModuleRegistry registry, PreparedSourceLiteralView819 literals)
    {
        this.template = template;
        this.registry = registry;
        this.literals = literals;
    }

    public void Register()
    {
        literals.BeginPublication819(registry, template);
        bool success = false;
        Exception? primary = null;
        try
        {
            template.RegisterLiteralView819(registry, literals);
            success = true;
        }
        catch (Exception error)
        {
            primary = error;
        }
        try
        {
            literals.EndPublication819(success);
        }
        catch (Exception cleanup)
        {
            if (primary is not null) throw new AggregateException(primary, cleanup);
            throw;
        }
        if (primary is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
    }

    public void Dispose() => literals.Dispose();

    public ValueTask DisposeAsync() => literals.DisposeAsync();
}
