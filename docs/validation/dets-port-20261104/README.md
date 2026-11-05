# DETS port and focused acceptance

Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
The C# implementation covers all 64 explicit `dets` exports and retains the
unchanged original Erlang/BEAM alternative. See [backend selection](../../dets-backends.md).
This record supersedes the unfinished Part564 draft; its original
[source inventory](source-inventory.json) remains historical evidence.
The [current inventory](current-source-inventory.json) records source and BEAM
hashes and imports; static inventory alone establishes no execution claim.

## Implementation

`Storage/Dets` contains the dispatcher, table worker, native v9 incremental file
engine, linear hashing, buddy allocator, persisted free lists, dirty/save
transitions, checksums, recovery/compaction, cache/autosave, ownership, fixation,
matching, continuations, bchunk, callbacks, ETS and QLC paths. Mutations do not
rewrite an entire compact file. Recovery copies collections through a temporary
disk file. Supporting original `dets_server`/`dets_sup` provide registry and
supervisor semantics on .NET, with no external ERTS.

Connected runtime changes supply declared native prim_file/prim_buffer resources,
the RAM driver protocol, raw file closure on owner death and authentic magic
reference lifetime. Guest heap collection remains authoritative. A supplemental
CLR sweep releases legacy managed interop payloads only when relevant weak
resources exist; dead BEAM registers/call arguments no longer retain them.

The original five modules contain 8,976 source lines and 166 explicit exports
in total (64 in dets). Source/BEAM assets and the sibling OTP checkout are
unchanged; counts do not certify semantics. Apache-2.0 notices are retained.

## Permanent acceptance

The primary authentic-BEAM observer runs 21 scenarios independently on C# and
original Erlang through real kernel boot and filesystem resources. It checks
set/bag/duplicate_bag, atomic insert_new, counters and complex numeric/map/bitstring
keys, deterministic mutations, scans, callbacks and full error stacks, QLC,
bchunk/ETS transfers, repeated owners, RAM/readonly access, nested fixation,
repair/compaction/corruption, owner-death flush, malformed headers, legacy
v9a/v9b files and native buffer behavior. Export equality and actual source
ownership are checked separately. Acquisition uses the same MFA on both VMs.

The unchanged pinned `dets_SUITE` is separately invoked for seven connected
cases: basic, insert_new, update_counter, fold, badarg, repair_continuation and
access. No CT/library test doubles replace those testcase bodies. A minimal
reference test checks four local/remote dead/retained compiled-match programs.
Native reference captures verify the admission manifest and executable inputs
before and after execution. Fixtures include source/BEAM/corpus hashes.

The standalone interchange tool invokes the production internal engine through
reflection, writes three native types and mutates three OTP-created files.
Official OTP opens all six and compares complete sorted rows, including
incremental 700-object growth. The final interchange receipt reports six passes.

Focused commands (repository root):

```powershell
dotnet build tests/Erlang.Tests/Erlang.Tests.csproj -m:1 --artifacts-path artifacts/dets-acceptance/build --no-restore
dotnet artifacts/dets-acceptance/build/bin/Erlang.Tests/debug/Erlang.Tests.dll --case runtime/dets/managed-public-otp-contracts --case runtime/dets/original-public-otp-contracts --case runtime/dets/managed-upstream-cases --case runtime/dets/original-upstream-cases --case runtime/dets/magic-reference-gc-contracts --report artifacts/dets-acceptance/final-dets.json
dotnet build tools/Validation/Erlang.DetsCompatibility/Erlang.DetsCompatibility.csproj -m:1 --artifacts-path artifacts/dets-acceptance/interchange-build --no-restore
dotnet artifacts/dets-acceptance/interchange-build/bin/Erlang.DetsCompatibility/debug/Erlang.DetsCompatibility.dll . artifacts/dets-acceptance/original-v10 artifacts/dets-acceptance/interchange-final
```

Historical failures are preserved under `artifacts/dets-acceptance`: missing
native file/RAM capability, readonly errno, callback stacks, damaged-file error
propagation and retained compiled-match references. Earlier `ninth-dets.json`
passed both primary backends while the upstream suites still failed; it is not
the final acceptance receipt. `eleventh-linked-gc.json` passes eight connected
lifetime/interpreter/ETS checks after the fixes.

## Final results

[Historical implementation receipts](implementation/README.md) record successful primary and
selected upstream acceptance on both backends,6/6 native interchange and8/8
connected groups. The initial failed managed upstream run remains unchanged;
its successful repair is a separate report.

## Limits

No full regression or canonical readiness refresh runs under the user's focused
check instruction. This is Windows evidence for a finite contract corpus,
selected upstream cases and native interchange. The complete upstream suite,
every scheduler/resource/failure interleaving, Linux/macOS and whole release
manifest startup remain unverified. A universal 100% compatibility claim would
exceed this evidence. There is no live backend switching or state migration.
