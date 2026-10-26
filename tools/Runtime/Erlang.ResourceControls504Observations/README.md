# Original OTP resource controls, batch 504

Source-only preparation. No VM, compiler, native build, CI, or SDK comparison was
executed by the preparing agent. Neither `statistics/1` nor `system_flag/2` is
promoted to full compatibility by this collector.

The five independent fresh-VM cases are `debug_flags`, `normal_queue`,
`held_queues`, `cancel_queued`, and `cancel_running`. The first checks fifteen
arbitrary second arguments for the unconditional release-source debug flag.
The other cases capture all ten queue selectors at every declared stage.
Native cases hold real CPU and IO bodies, then queue a second body of each kind.
Separate fresh VMs observe queued cancellation and running-owner cancellation.
The reducible normal loop measures actual normal queue activity without blocking
the observer. Its physical numbers are retained rather than replaced by constants.

Native bodies have a twenty-second deadline; an Erlang `after` releases them.
The collector owns each compiler and VM process group with the existing sealed
`OwnedProcess` helper, closes streams, reaps the process, and retains cleanup
receipts. Successful capture refuses forced cleanup. Cases continue independently
after failures; a native build failure skips dependent native cases but still
runs debug flag and normal queue cases. All failures aggregate into the final
nonzero exit. Failed preparation is never treated as an executed case.

Admission reuses the existing `PASS_OWNED_PINNED_ORIGINAL_BUILD` receipt from
`Erlang.SchedulerAffinityOriginalObservations/build-original.py`. It checks the
original manifest identity, exact subject head, pinned clean tracked source,
full before/after source inventory, all build-stage receipts and streams,
installed executable/module hashes, OTP version and installed NIF headers.
It does not compile another OTP installation. The runtime root and scheduler
profile are also checked inside every fresh original VM. The tool dependencies
use declared LF text hashes; actual raw source hashes are retained in inputs.

Seal after all source edits (a source-only operation):

```bash
python3 -B tools/Runtime/Erlang.ResourceControls504Observations/seal-plan.py --repository "$GITHUB_WORKSPACE"
```

Using an existing built original installation in the same job:

```bash
receipt="$GITHUB_WORKSPACE/artifacts/nif-entry-original/runtime-build/build-receipt.json"
receipt_sha=$(sha256sum "$receipt" | cut -d ' ' -f 1)
python3 -B tools/Runtime/Erlang.ResourceControls504Observations/collect.py \
  --repository "$GITHUB_WORKSPACE" --expected-head "$GITHUB_SHA" \
  --plan-sha256 "$RESOURCE504_PLAN_SHA256" \
  --otp-source "$GITHUB_WORKSPACE/artifacts/nif-entry-otp-source" \
  --otp-install "$GITHUB_WORKSPACE/artifacts/nif-entry-otp/lib/erlang" \
  --build-receipt "$receipt" --build-receipt-sha256 "$receipt_sha" \
  --output "$GITHUB_WORKSPACE/artifacts/resource-controls504-original"
```

The parent owns workflow integration and dispatch. Upload the entire fresh
output directory even on failure. No existing workflow is modified by this tool.
The source plan is independent of the NIF map family plan.

`collection.json` reports only original acquisition and typed controls. Raw
outcome ETF includes complete stacks on errors. Per-stage ETF, typed measured
queue values, profile, preparation/VM stdout/stderr and process receipts remain
available. Sequential queue-selector calls cannot prove equality for moving
normal queues, so the guard never compares them to a fixed total. During stable
held native stages, dirty queued and active counts are checked exactly. After
cancellation the raw values remain observations until compared with the SDK's
physical queue model. Actual collection does not imply full resource-control,
term-heap, GC, scheduler-wall-time, or native loader compatibility.
