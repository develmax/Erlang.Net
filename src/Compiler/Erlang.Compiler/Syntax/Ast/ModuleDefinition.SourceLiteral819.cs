namespace Erlang.Compiler;

public sealed partial record ModuleDefinition
{
    public PreparedSourceModule819 PrepareSourceModule819(ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _ = registry.RequireSourceLiteralPreparation819();
        var literals = new SourceConstantPool820(this);
        PreparedSourceLiteralView819 prepared = registry.PrepareSourceLiteralView819(this, literals.Values, literals.Patterns);
        try
        {
            prepared.BindConstants820(literals.Slots);
            prepared.BindNativeRecords323(NativeRecords);

            return new(this, registry, prepared);
        }
        catch
        {
            prepared.Dispose();
            throw;
        }
    }
}
