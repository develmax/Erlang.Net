namespace Erlang;

// Private measured walker/lease diagnostics, never public ERTS resource metrics.
internal readonly record struct ProcessBinaryTraversalSnapshot514(
    long VisitedTerms,
    long RetainOperations,
    long LiveBackingReferences,
    long PeakBackingReferences
);
