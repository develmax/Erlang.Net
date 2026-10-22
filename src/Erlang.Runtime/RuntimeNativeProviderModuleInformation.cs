namespace Erlang;

internal sealed class RuntimeNativeProviderModuleInformation(ModuleRegistry owner, string module)
{
    internal ValueTask<Term> All(ProcessContext context, IReadOnlyList<Term> arguments)
        => Invoke(context, arguments);

    internal ValueTask<Term> Field(ProcessContext context, IReadOnlyList<Term> arguments)
        => Invoke(context, arguments);

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime.Modules, owner))
            throw new ArgumentException(RuntimeNativePrimitiveNames.ForeignRegistry, nameof(context));
        // Original native modules retain their module_info frame when the
        // metadata BIF rejects a key. Preserve this actual provider frame.

        return await BeamGeneratedMetadata.Invoke(context, module, arguments);
    }
}
