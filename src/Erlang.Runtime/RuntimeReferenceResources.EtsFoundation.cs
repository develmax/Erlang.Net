namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateEtsFoundation(RuntimeEtsFoundationTable table)
        => Create(ReferenceResourceKind.EtsTable, table);

    internal ReferenceResourceLease<RuntimeEtsFoundationTable>? AcquireEtsFoundation(ReferenceTerm reference)
        => Acquire<RuntimeEtsFoundationTable>(reference, ReferenceResourceKind.EtsTable);
}
