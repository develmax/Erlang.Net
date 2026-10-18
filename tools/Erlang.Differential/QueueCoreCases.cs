namespace Erlang.Differential;

public static class QueueCoreCases
{
    private static Term Completed => Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion));

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(QueueCoreCaseNames.Empty, QueueCoreCaseNames.EmptySource, Completed),
        new(QueueCoreCaseNames.Representation, QueueCoreCaseNames.RepresentationSource, Completed),
        new(QueueCoreCaseNames.Persistence, QueueCoreCaseNames.PersistenceSource, Completed),
        new(QueueCoreCaseNames.RawShapes, QueueCoreCaseNames.RawShapesSource, Completed),
        new(QueueCoreCaseNames.Improper, QueueCoreCaseNames.ImproperSource, Completed),
        new(QueueCoreCaseNames.Ordering, QueueCoreCaseNames.OrderingSource, Completed),
        new(QueueCoreCaseNames.Errors, QueueCoreCaseNames.ErrorsSource, Completed),
        new(QueueCoreCaseNames.Heterogeneous, QueueCoreCaseNames.HeterogeneousSource, Completed),
    ];
}
