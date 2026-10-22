namespace Erlang;

public sealed partial class ModuleRegistry
{
    private bool TryCaptureRegex413(
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        var prerequisite = (ReNames411.Module, ReFacetNames411.Compile, 2);
        bool signature = arity == 2 && function == ReExtendedNames413.Inspect
            || arity == 1 && function == ReExtendedNames413.Import
            || arity == 4 && function == ReExtendedNames413.InternalRun;
        if (module == ReNames411.Module && signature && owningRuntime is not null
            && IsNativeEntry(prerequisite.Item1, prerequisite.Item2, prerequisite.Item3)
            && !sourceOwners.ContainsKey(prerequisite)
            && functions.TryGetValue(prerequisite, out var installed)
            && installed.Target is ReNativeBinding413 binding
            && ReferenceEquals(installed, binding.Body)
            && ReferenceEquals(binding.Engine.Owner413, owningRuntime))
        {
            body = new ReNativeBinding413(binding.Engine, (function, arity)).Body;

            return true;
        }
        body = null!;

        return false;
    }
}
