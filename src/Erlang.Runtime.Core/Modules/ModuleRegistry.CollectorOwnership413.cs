namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Selected by the actual independent-Term BEAM literal reader/backend.
    // This is not a count of hypothetical retired ERTS literal areas.
    internal ManagedLiteralStorageModel413 CollectorLiteralStorage413 { get; }
        = ManagedLiteralStorageModel413.IndependentManagedTerms;
}
