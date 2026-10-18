# Queue callback cancellation-entry correction

The first actual later-phase HOST found a real production gap. Public Exit uses direct Signal/Terminate and Stop.Cancel; the earlier queued-signal explanation was incorrect. A completed ReduceAsync cancellation check could precede the queued consumer continuation, allowing a callback to enter after that continuation was resumed with its token already canceled.

The reviewed fix adds genuine zero-charge cancellation/halt/suspension admission at QueueHigherTrace.Invoke entry, before callback frames and invocation. The repaired HOST counts actual callback entries with an already-canceled token and retains canceled/killed/physical-unwind requirements. This is cooperative entry admission, not preemption of an already-running callback.

Exact source and original failure-report hashes are bound. This review is source-only; final HOST execution, native replay and guard refresh remain Root gates. No Main edits, builds or runtime execution were performed by reviewer.
