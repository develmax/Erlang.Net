# Queue higher-operation later-phase HOST proposal

Four new test classes/files and one HOST registration add16 independent normal/cancellation controls. Current core test files, Program and runtime remain untouched. This proposal is source-only and has not been built or executed. Root owns style, integration and actual validation.

The real continuation pump stops after public callback counters establish a specific source phase and a real production continuation is queued. Front filter and last-front-match delete have completed their callback scan before pausing during reconstruction. Rear filter starts callbacks only after full collection; first-rear-match delete has entered reconstruction with further callbacks suppressed. Separate no-match controls are deliberately labelled scan/unwind: no-match returns the original queue rather than reconstructing it. Large split/join test genuine first-yield paths without claiming a later phase.

Each scenario runs normal and kill. Normal outputs and exact callback counts are checked independently; no-match requires the same original queue object. Cancellation requires actual killed completion, canceled operation, finally/physical unwind, no further callbacks, dead process and a still usable runtime. The pump runs actual captured callbacks, creates no fake async callback, sleeps or GC assertions, and uses bounded waits. All16 outcomes aggregate.

Register once:

```csharp
Test(Erlang.Tests.QueueHigherCooperativeHostNames.Test, Erlang.Tests.QueueHigherCooperativeHostChecks.Run);
```

All four destinations must be absent. The copy plan binds current runtime loop locations and actor helper source assumptions; a changed implementation requires phase applicability review. Existing runtime source/guards need no refresh for this test-only packet.
