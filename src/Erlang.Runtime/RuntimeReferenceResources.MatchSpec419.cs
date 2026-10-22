namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateMatchSpec419(EtsCompiledProgram419 program)
        => Create(ReferenceResourceKind.CompiledMatchSpecification419, program);

    internal ReferenceResourceLease<EtsCompiledProgram419>? AcquireMatchSpec419(ReferenceTerm reference)
        => Acquire<EtsCompiledProgram419>(reference, ReferenceResourceKind.CompiledMatchSpecification419);
}
