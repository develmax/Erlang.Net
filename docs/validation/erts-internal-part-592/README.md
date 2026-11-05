# Part592 — real multi-scheduling blockers

Date: 2026-11-05. Reference: OTP-29.1.1,
`ad05823719d77c8faee87348ea39513d4e2f99c5`.

The Part586 multi-scheduling gate is closed. Both selected implementations of
`erts_internal` now pass the previous failing `get_internal_state_blocked/1`
case and the expanded focused acceptance. The native `system_flag/2` handles
`block`, `unblock`, `block_normal`, and `unblock_normal`; `system_info/1` exposes
the committed state and the two unique blocker lists.

| Check | Actual result |
| --- | --- |
| Clean scoped build of both validation tools | 0 warnings/errors |
| `multi-scheduling-release-oracle/Managed-report.json` | 262/262 complete primary observations; 45/45 scenarios |
| `multi-scheduling-release-oracle/Beam-report.json` | 262/262 complete primary observations; 45/45 scenarios |
| Nested operation sequences within the scenario corpus | all 341 sequences of length 0–4 |
| `multi-scheduling-release-physical.json` | 10/10 real physical scheduling/lifetime groups |
| `connected/connected.json` | 36/36 connected regression groups, explicitly partial |
| SDK source-layout check | 0 files need formatting |
| Full regression, validate.ps1, canonical readiness | deliberately unrun |

All finite checks in this matrix pass. This is **not** a universal 100% OTP or
whole-module certification. The separate native debug inventory for
`node_and_dist_references` remains unimplemented: the source-audited
`ProcessRuntime.DebugInternal419.cs` still rejects that recognized partition.
That pre-existing dependency, full upstream/native-debug/resource/platform and
distribution acceptance remain outside the completed blocker closure. No
fabricated inventory or reference counts are returned to hide this boundary.

## Implementation and invariants

Normal scheduling changes use the existing actual actor barrier. The block call
waits for previously selected normal work and active dirty calls, then directs
unbound guests to scheduler one. Strongly bound processes on suspended lanes
remain parked until unblock. Configured online counts and bindings are retained.
`block_normal` does not stop dirty CPU/IO work.

Dirty CPU and shared-IO owners stop dequeueing guest invocations. A snapshot of
already-running settlement tasks proves physical completion. A pool first
created during a block inherits suspension before publication/enqueue. Resume
restores missing queue wakeups without multiplying semaphore credits. Shutdown
unparks closing queues so cancellation and physical joins cannot strand work.

Each process owns separate nested all-scheduler and normal-only counts. Its
cancellation registration releases every owned block on normal death, kill or
shutdown. Pending intent suspends new dirty admission but does not masquerade as
an achieved block in state/list queries. The change permit is released after
publication, before requester reentry; the native return value is captured at
the completed transition. Exception paths undo unfinished reservations.

The actual unchanged original `erts_debug.beam` is loaded in the differential
runner. Disabled and enabled internal-state failures compare full reasons,
arguments, source frames and error metadata. The C# helper now preserves source
locations 986/988/991 for block, native read and finally-unblock. The original
Erlang/BEAM module is unchanged. The selected surface remains 98 exports:
27 C# library signatures and 71 shared native signatures; this follow-up adds
no MFA registrations.

## Commands and source binding

```powershell
pwsh -NoProfile -File tools/Validation/Erlang.ErtsInternalCompatibility/build-scoped.ps1 -Output artifacts/multi-scheduling-release-scoped
pwsh -NoProfile -File tools/validate-erts-internal-original.ps1 -Output artifacts/multi-scheduling-release-oracle -CaptureOnly
dotnet artifacts/multi-scheduling-release-scoped/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/multi-scheduling-release-oracle Managed
dotnet artifacts/multi-scheduling-release-scoped/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/multi-scheduling-release-oracle Beam
dotnet artifacts/multi-scheduling-release-scoped/snapshot/tools/Validation/Erlang.MultiSchedulingCompatibility/bin/Debug/net10.0/Erlang.MultiSchedulingCompatibility.dll artifacts/multi-scheduling-release-physical.json
```

Use new output paths when reproducing. The scoped builder starts from
`20e02eb941ef3b99cb675827ef0f20663da7c04b`, copies only the owned port/blocker
files and applies three explicit startup/statistics integration edits. It
excludes unrelated dirty ports and generated bin/obj directories. `sources.json`
binds all changed C# files to the tested scoped sources. Reports contain exact
runtime, runner, observer, corpus and original BEAM hashes, including erts_debug.
The development provider checks the admitted execution manifest and all used
OTP executable/source/BEAM files before and after capture.

The connected run uses the existing prebuilt test harness with matching scoped
runtime/terms/serialization/stdlib assemblies in a new output directory.
`connected/command.json` contains the exact 36 selected names; binary identities
are in `connected/assemblies.json`. It is not a rebuild or execution of the full
catalog. Selected groups cover original internal primitives, statistics-trap
failure/cancellation/drain, five dirty-owner lifetime groups, three actual normal
barrier groups and eleven scheduler binding/online/cancellation groups.

## Retained discovery

Initial 262/262 plus 35/35 acceptance closes the old single failing row. The
expanded sequence/binding/death corpus reaches 43/43. Loading real erts_debug
then exposes two C# source-frame mismatches (985 versus 988); these are fixed,
and the final 45/45 includes both disabled and enabled error paths.

The oracle itself times out when an experimental requester is strongly pinned
to scheduler two and calls block. These attempts are retained under
`multi-scheduling-oracle-1` and `multi-scheduling-oracle-4`; they are not admitted
as finite passing cases or silently retried. The finite binding test instead
checks a separate pinned process waiting through another process's valid block.
No expectation is changed to make the earlier source-frame mismatches pass.

An initial connected-harness attempt combined a current runtime with an older
stdlib stage enum and failed before test execution. A fresh directory with
matching assemblies produces the 36/36 receipt. Earlier observations, failed
builds and native warnings are retained, not substituted for final-head results.

The previous immutable comparison is [Part586](../erts-internal-part-586/README.md), where
both alternatives were 262/262 and 34/35. Canonical readiness is preserved;
the partial connected report must not replace its full comparison snapshot.
