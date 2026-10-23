namespace Erlang;

// Trusted managed owner callback, invoked after admission closes but BEFORE
// active-call drain. Must only initiate cancellation; no blocking join.
internal interface INifImageRetirementPreparation
{
    void BeginRetirement();
}
