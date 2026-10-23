# Part453 — original Unicode traps and named kernel boot

Pinned original: **OTP29.1.1**, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

This increment implements two observed runtime fixes and executes the original `init`/kernel/application/`net_kernel` chain with a real node identity. The accepted profile explicitly uses **`dist_listen=false`, `no_epmd`, `proto_dist=inet_tcp`, `-sname managed453@localhost`**. It has no TCP listener or EPMD registration. Full original-server peer traffic and wider transport races remain open.

## Accepted results

| Area | Final evidence | Scope boundary |
| --- | --- | --- |
| Unicode `re` |210 complete original outcomes:162 strict source-raw rows,48 reference-only resource bijections;48 SAME error-argument patterns authenticated by inspect/run |35 scenarios in six profiles, not all regex options/API contracts |
| Actual active conversion cleanup |Four compile/run × kill/owner-close profiles; measured original re+unicode leases, reductions and active engine work; balanced frees/unload and no remaining leases |Managed ownership/lifecycle, not physical BEAM GC or performance parity |
| Original named kernel |144 whole typed outcomes and full stacks;138 strict source-raw rows,6 map-order-only state rows; original source owners and live registration checked |Both strict original whole-file comparisons are false; no field projection or fabricated byte-equality claim |
| Final affected regression |607 unique cases accepted from immutable final inputs:606 initial successes plus the complete failed20-case block passed twice |Initial607 run remains failed; see the actual child CoreCLR startup OOM below |
| Native/DDLL retention |Eight actual native groups and retained36 whole original outcomes pass on the current package |Existing scoped ABI/visibility proof, not complete arbitrary-driver parity |
| Prior standard boot |79/79 plus actual collector/persistent-term cleanup; all6427 provider files unchanged |Separate from new named boot144 |
| Build/style/readiness |Final formatted build7 has0 warnings/errors; five focus groups pass; full source guard validation and style pass |Catalog8833 is not a local full8833 run |
| Registry |708 APIs/118 verified full contracts unchanged;1621→1623 source guards; API/full deltas0/0 |31 Known differences and559 Needs verification remain |

Actual final runtime SHA256: `F39E2D87B336CC45D2706B5B63F097E269C594A775233B52DBE12156CA87D7C1`.

Actual rebuilt driver package manifest SHA256: `3D72B5324BA4A80091D1C5894B4D55FEA2FDBACDB03D031C2742E472C1826A5A`. The native bridge is reused only after checking its genuine original compile receipt and unchanged source hashes; the managed child package is rebuilt against the current runtime.

## Product changes and original authority

- `RuntimeRegexEngine411.Operations.cs`, `.Unicode453.cs` and `ReUnicodeTrap453Names.cs` follow the exact `erl_bif_re.c` Unicode trap predicates. Conversion and error/stack construction execute unchanged original `re:ucompile/2`, `re:urun/3` and `unicode`; no pure Erlang wrapper is rewritten in C#. Missing/non-BEAM wrapper owners are refused. Ordinary binary/native paths remain covered.
- `ModuleRegistry.BootstrapFileSystem.cs` recognizes canonical filesystem primitives after their ownership transfers to the current original BEAM image. Require the actual current image, exact captured native body and actual exported wrapper. Unrelated stand-ins do not gain filesystem admission. Previously the original `prim_file:on_load` attempted a nonexistent external `prim_file.dll` after whole `erts_internal` publication.
- Named boot composes the existing opt-in internal-port, atomics and ETS inspection providers and early original `re` (8 native bodies), `erts_internal` (71) and `erts_code_purger` (0). Verify original `net_kernel:init/1`, `init:boot/1`, `erts_internal:spawn_system_process/3`, purger `start/0`, the real registered Pid and `is_alive=true`.

The original `crypto:on_load` warning is retained: current `system_info(build_type)`/crypto native prerequisites are incomplete; original `net_kernel` catches creation setup failure and uses its own original random fallback. Do not confuse this successful fallback profile with full crypto/logger/socket compatibility. The original registry, UDP and TCP builtin drivers remain subsequent transport prerequisites.

## Raw map order and stacks

All144 typed terms match, including class, reason, error arguments, filenames, source lines and every stack frame at default/0/1/2/8. The independent parser also compares literal original ETF row slices:138 identical; only the six `state` rows reorder the same map entries. Original order is `name,started,name_domain,name_type`; managed order is `name,name_domain,name_type,started`. Every raw non-map leaf and list/tuple header is unchanged. Both complete original raw files remain unequal and are retained as such. See [the independent review](kernel-map-order-review.md) and [row audit](kernel-map-order-audit.json).

The regex reference protocol is narrower: only the final opaque reference of an actual `re_pattern` may differ, with a bijection and exact node/creation. Everything else remains exact. Actual error-argument resources are reused and authenticated, never replaced by compiling a new pattern. See [review](review.md), [independent210-row audit](passive-audit.json) and final permanent receipts inside the archive.

## Failures and recovery, without masking

- Initial port block9 records one `no_aux_echo`. Its own stderr proves the child failed to load `System.Private.CoreLib.dll`/create CoreCLR: **Out Of Memory, `0x8007000E`**. It did not start the echo application. The initial report remains606/607 and the aggregate gate remains failed. On identical sources and DLLs, the **entire20-case block** then passed twice with empty stderr. Accept the607-case union explicitly; do not call the initial run green. No port source, expectation or timeout was changed.
- Original-provider attempts with missing or misspelled `proto_dist`, missing native options/preloads, initial Unicode atom predicates, an over-broad resource comparator, and the incorrect actor-completion-only success guard remain retained. The false guard result was never accepted as named boot.
- Permanent boot initially exceeded the generic15s test budget; it now has a dedicated bounded150s outer budget. A later boot-ready cancellation is retained. Successful integration joins ephemeral on_load work before boot and publishes the observer/starts its persistent host actor afterwards. Two ordering changes were made together; no isolated cause is asserted for the earlier timeout. A conditional host Channel-versus-Mailbox GC concern remains an investigation, not a proved product defect/fix.
- A strict whole-file comparison correctly refused the map order variation; the final receipt separates full typed equality from original raw-file inequality. The first proof finalizer also refused because a regex-only comparator was used on maps; it was corrected to reuse the independently audited map-order-only algorithm before any archive was accepted.

## Merges and stale branch cleanup

[PR12/451](https://github.com/develmax/Erlang.Net/pull/12) and [PR13/452](https://github.com/develmax/Erlang.Net/pull/13) are merged. Exact main `b161ea9b421d71673f7d9550b2de9426c38dbd06` independently passes [CI39978960949](https://github.com/develmax/Erlang.Net/actions/runs/39978960949):8828/8828, every mandatory step and both packages.

Delete13 old merged local/remote branches only after exact tip/reachability/open-PR/protection/active-worktree checks and a verified recoverable Git bundle: `feature/native-441` through `452` and `feature/otp-foundations-363-370`. Preserve main and the active `validation/ordsets-erlang-372` worktree branch. The local bundle SHA is `F474912A94B20C64AF835390E5AB630AA7A32B77D8B4D4E8344BB9686A7C59CA`; do not duplicate the full bundle in this Git proof. Exact plans, checks, deletion receipts and bundle verification are inside the archive.

## Retained proof and reproduction boundary

[Acceptance](acceptance.json), [831-file manifest](files.json), [raw evidence archive](raw-evidence.zip). Every archived member has a byte length and SHA256; the archive itself has a SHA256 in `files.json`. Failed and successful attempts remain separate. Current permanent test resources preserve original source/BEAM and original ETF with per-file manifests. The fresh original144 repeat verifies all6427 provider files before/after and equals the earlier whole worker BEAM/ETF files.

Root-owned recipes are archived under `artifacts/library453-*.py`; actual commands, deadlines, raw stdout/stderr and settled process receipts are retained. The final probe source is retained, while earlier probe revisions were not independently frozen; old raw failure receipts do not imply reconstructible old source closures. Recipes depend on the recorded admitted Windows provider/native artifacts and are not a new clean-checkout source-snapshot contract. The historical8813 readiness authority stays separate. Git publication requires rebuilt exact-head full CI and both packages; local accepted607 is not full8833.

No generated license headers were introduced. Upstream reference copyrights are preserved. `LICENSE.local.txt` is excluded. No NuGet release, whole-module/full-contract or performance promotion is implied. Next: original TCP/UDP/registry driver integration, listener/EPMD and real original-server peer traffic, then wider checker/timer/owner races.

Publication metadata supplement: the compatibility ledger retains its original raw CRLF format to avoid an unrelated whole-file diff. All classifications and source guards are identical to the accepted input; runtime/test sources and binaries are unchanged. Readiness was regenerated against those final metadata bytes. See publication-metadata.json and publication-readiness.json.
