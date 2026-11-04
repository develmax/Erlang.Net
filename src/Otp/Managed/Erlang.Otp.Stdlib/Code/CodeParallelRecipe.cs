namespace Erlang;

internal sealed record CodeParallelRecipe(RuntimeFunctionEntries Owner, bool ItemWorker) : IRuntimeStoredFunctionRecipe
{
    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(CodeModule.Name),
            Term.A(ItemWorker ? CodePreparationProtocol.ItemWorkerFunction : CodePreparationProtocol.CoordinatorFunction),
            CodePreparationProtocol.FunctionIndex,
            CodePreparationProtocol.FunctionOldUnique,
            new BitString(new byte[FunctionCodeLayout.UniqueBytes])
        );
        bool itemWorker = ItemWorker;
        var function = new FunctionTerm(
            identity,
            CodePreparationProtocol.FunctionArity,
            environment,
            (context, _) => CodeModule.RunParallel((ProcessContext)context, environment, itemWorker)
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}
