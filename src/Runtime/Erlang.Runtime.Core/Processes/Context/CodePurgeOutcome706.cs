namespace Erlang;

// Separate physical retirement from the code-server result (whether hard purge
// actually issued a process kill and observed its DOWN/unwind).
internal readonly record struct CodePurgeOutcome706(bool Purged, bool DidKill);
