# DDLL current-boundary serial observation proposal443

Status: sealed source/acceptance preparation only. No product/test/tool/docs edit, compiler, native DLL, original OTP VM, own VM, git or CI execution by the helper. No new implementation or compatibility credit. Root alone executes/integrates. Existing435/438/441 sources and receipts remain immutable.

## Exact bounded question

At OTP29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`, `io.c:7646` stores the old head in the newly allocated outer descriptor's `dp->next`. Nested `remove_driver_entry` updates the actual predecessor's `next` (`7704–7709`). Failure resets the global head to **current** `dp->next` (`7669`), not a saved original-head address. This avoids the suspected freed-boundary read: deleting immediate C updates A.next to D; deleting deeper D updates C.next; deleting both updates A.next to the still-live dynamic root.

Current `ErlDriverLinkedServices420.Initialization435.cs` removes new prefix rows until the first surviving old row (captured by identity HashSet). Under the actual prepend/remove-only list mutations this appears observationally equivalent. No source-proven production discrepancy was found and this packet contains **no runtime patch**. The newly captured original results may confirm or disprove that conclusion; no expected successes/list values are manufactured.

Original C failure disconnects nested prefix descriptors without explicitly destroying those orphaned descriptor allocations (only failed dp is destroyed). Own runtime releases actual disconnected descriptor/image-worker leases. This is an intentional ownership safety distinction, not claimed identical physical allocations or original leak behavior. No memory metric/projection appears in the oracle.

## Native and source worker

A genuine dynamic driver `ddll_boundary443` is loaded through original erl_ddll. Its actual native start adds old D then C, giving C,D,root. Its control callback adds linked outer A; A.init adds nested B, removes none/C/D/both and succeeds/fails. Native init failure remains the same exceptional add-driver path used by the already admitted435 fixture; `add_driver_entry` is void and intentionally ignores its return in OTP. This is a captured exceptional source branch, not a promise that failing init is recommended driver-author behavior.

All actual driver-list calls occur in native start/control/stop. No dynamic entry.init nested-add deadlock, finish-list-lock deadlock, OS sleep, synthetic driver service or fabricated descriptor. Stop removes all four own static entry pointers, tolerating genuine missing descriptors. Never remove the dynamic root using remove_driver_entry.

Eight serial cases: success/failure × keep/immediate/deeper/both removal. For each case the identical compiled Erlang worker records actual try_load return, complete own-name driver presence flags, **actual order** of these five names in loaded_drivers, native control result bytes, actual unload result and post-unload names/order. It does not return port/PID/reference values. The worker additionally captures two real badarg load/unload-option cases, preserving all native full stacks and lines. The own-five-name filter is an explicit projection inside identical Erlang observer source in both VMs. The complete resulting source-observer terms and ETF bytes are compared without any comparison-side projection or normalization; there is no complete loaded_drivers public-term parity claim. Root must investigate any native crash/hang or unexpected error; it is not an expected success or a reason to rewrite goldens.

Source child launch, default/0/1/2/8 backtrace depth, actual monitored observer settlement and cleanup reuse435. Cases run once as ordinary values and five times with stacks: **10+50=60** complete rows. Values and stacks are independent original VMs after shared compilation, so one route's failure does not suppress the other. Baseline original228 is not rerun; its exact stock erl_ddll BEAM is authenticated/reused for managed preparation.

## Root original recipe

Use the same admitted executable hashes and bounded toolchain environment as actual441. Do not substitute mutable toolchain paths silently.

```
python -B artifacts/library443-ddll-boundary-proposal3/collect-original-root.py --repository . --output artifacts/library443-ddll-boundary-original1 --compiler <admitted-cl.exe> --compiler-sha256 <actual-admitted-sha> --toolchain-environment <admitted-environment.json> --toolchain-environment-sha256 <actual-admitted-sha>
```

Collector uses sealed351 owned-process120s runner, admitted64KiB426 inventory, official admission and exactly two complete scans of the6427 installed-provider tree: before the complete batch and in its final finally, including failed owned stages. No provider cache or per-stage full scan. Bound source inputs are rehashed before and after every owned stage; all raw owned receipts remain durable. Actual native and Erlang compilation proceed independently; VM capture depends on both successful preparations. Both capture routes run independently and full failures are aggregated. There is no fake facet-compilation stage. Exact source/input hashes, raw stdout/stderr, child settlement, native DLL, observer BEAM, values.etf and stacks.etf are durable. Passive verifier checks only exact case names/order, whole-row/result/error schema, five profile shapes and absence of opaque identities; it does not construct expected snapshots.

## Root managed recipe

Create a genuine fresh package against the current final Runtime/Terms/Serialization assemblies. Do not update old441 package hashes. The existing `artifacts/library433-port-parent-input-proposal1/prepare-parent-input.py` accepts a fresh successful package build/receipt and the retained actual port150 original, producing a current authenticated one-normal-lane parent input. A separately source-bound fresh Root parent input with actual3 normal lanes is also accepted; the harness constructs the exact reported capacity. Its purpose is input preparation; it does not execute the old port cohort here. The package441 command/receipt/config shape are retained at `artifacts/library441-driver-package1`; a general reusable sealed package-build producer was not located. Root must retain a fresh actual build receipt with unchanged full source closure and matched assembly bytes.

```
python -B artifacts/library433-port-parent-input-proposal1/prepare-parent-input.py --repository . --package-build <fresh443-package> --runtime-directory <current-tests-bin> --original artifacts/library428-internal-port-loader-original2 --output artifacts/library443-parent-input1
python -B artifacts/library443-ddll-boundary-proposal3/run-managed-root.py --repository . --fresh-parent-input artifacts/library443-parent-input1/parent-input.json --runtime-directory <same-current-tests-bin> --original artifacts/library443-ddll-boundary-original1 --output artifacts/library443-ddll-boundary-managed1 --dotnet <admitted-dotnet.exe> --dotnet-sha256 <actual-admitted-sha>
```

Managed harness reuses the exact seven sealed real parent helpers from422. It publishes the original stock erl_ddll with actual private-native facet7 and exact source owner, then publishes the exact new worker BEAM with explicit imports. It executes both routes independently through the real actor/parent IPC/complete native child backend. Both raw ETF bytes and full typed terms are compared wholesale only after real runtime/driver owner disposal. Actual child PhysicalExit/PhysicalCleanup/Completion and existing ZeroLeases assertions must all pass. No golden replacement, catch-all fault conversion, partial table or new production hook. Packet six-source bindings are review preconditions, not code replacements; refusal on drift needs a newly reviewed binding, never auto-refresh.

## Acceptance / limits

Required Root result: successful original60 with all4 real preparation/capture stages, provider6427 unchanged across its whole batch; managed2/2 whole typed/raw files; actual child physical settlement and zero real owner leases. Existing serial36/held8 and affected/native-owner regressions remain independent and are not implied by this packet. No full erl_ddll compatibility promotion or OS/ABI/physical-memory parity. No concurrency, cross-image removal or failed-init callback-thread matrix is added here: those are separate441 scopes.

All authorities retain original license headers. `source-bindings.json` documents actual Main source and sealed fixture lineage examined. Root owns permanent-test integration later; this standalone consumer does not edit tests/Program or embed unexecuted expectations.

## Successor2 orchestration-only delta

Native C, Erlang worker and all three C# harness files are byte-identical to sealed proposal1; exact hashes are recorded in successor-delta.json. Profile/case60 and all source/ABI/dependency/parent authentication gates are unchanged. Old proposal1/receipts remain untouched.

Each runner now performs exactly two complete non-cached6427 scans per entered execution batch (before/after); the after scan runs from finally after a source mismatch, stage failure, timeout or comparison/parser error. A failed initial scan prevents dependent execution and still attempts the after scan. Provider mismatch or scan failure is explicit aggregate failure, never hidden by successful stage flags. There is no claim of per-stage full-provider attribution: intermediate provider mutation restored before the final scan is outside the batch-envelope evidence, while all actual bound source inputs are still checked per stage. Root must keep the provider exclusively owned/immutable during each batch, as for the existing accepted collector.

Original native/observer preparations are independent, both VM routes require successful preparation and proceed independently of one another. Managed execution requires a successful harness build and byte-identical actual Runtime; its original golden files and actual cleanup assertions remain untouched.

Nominal complete-scan counts from source: original runner9→2 (four owned stages), managed runner5→2 (two owned stages). New provider-batch-checks.json retains each real scan duration, equality and failure; old receipts are preserved for Root duration comparison. **No timing improvement has been measured by helper**, and no compiler/native/VM execution took place. This is solely a bounded orchestration optimization, not a compatibility increment.

Successor3 changes only this README precision after successor2 sealed. All executable scripts, native/observer/harness payloads and source bindings remain byte-identical to2;2 is preserved.
