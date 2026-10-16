# Genuine original busy distribution observations (323)

Source-only collector, not executed against OTP or the SDK by its author. Its two cases each use fresh source-built OTP29.1.1 A/B/C VMs and a foreground private epmd. It measures public process_info and C-origin public EXIT2 kill while the target guest is suspended on actual A-B busy distribution. A stopped B process, filler event, idle receiver, simulated event or suspend_process alone cannot satisfy the target-specific busy prerequisite.

The collector validates the clean exact source pin, completed build receipt/log binding, installed root/version and actual launcher/engine hashes. It invokes the actual source-built erlc with -Werror +debug_info for exactly two Erlang modules. All three roles attest both loaded image MD5s, their actual OS PID and the actual 1024-byte busy limit. No BEAM image is rewritten. Each new session binds unique node names/references/nonces, PID=PGID and the B /proc start-time fingerprint before SIGSTOP. It requires confirmed T state before releasing the filler, and repeated T observations through the completed owner query and fresh killed DOWN before SIGCONT.

Payload saturation is bounded to1024 attempts of65536 binary bytes (64MiB logical payload, not physical/wire memory), with8s busy acquisition,4s query/kill deadlines and an absolute20s stopped-B limit. Both original send and link targets require their own real busy_dist_port event on B's actual controller, async_dist=false, status suspended, exact remote link and no post-call marker. Native already-buffered signals are not assumed absent after death. Query and killed proof have separate raw captures and the C call has its own independent receipt. Original Erlang untrappable kill needs no user finally acknowledgement.

Three322 Python helpers are retained byte-exact. The additive passive_control_etf.py is the same bounded decoder with precisely PORT_EXT102, NEW_PORT_EXT89 and V4_PORT_EXT120 branches, preserving actual node/ID/creation/tag. Legacy and new IDs are28 significant bits, legacy creation2 bits, new creation32 bits, V4 ID64/creation32 bits. Exact pinned external-format documentation and the extension diff are bound by the artifact freeze. No port-to-string projection or raw outcome normalization exists. Synthetic decoder/verifier controls are distinct from native execution.

On every failure the collector attempts SIGCONT for its exact possibly stopped owned B before any graceful cleanup/close. It writes nonce-bound cleanup markers and allows at most3s of remaining active session time for the role drivers to freshly join their local workers. It then always closes/reaps all owned groups with the unchanged helper's bounded TERM/KILL path. Target fresh cleanup noproc never substitutes for the earlier required killed DOWN. Primary errors and cleanup errors remain separate; raw ETF, streams, exit/process receipts and parent phase journal remain in the evidence directory. An uncatchable external SIGKILL cannot run Python finally; no stronger guarantee is claimed.

Run only after source review in the approved Linux environment with the existing pinned build:

```sh
python3 -B tools/Erlang.DistributionControlWaitObservations/collect.py \
  --otp-source "$GITHUB_WORKSPACE/artifacts/beam-otp-source" \
  --otp-root "$GITHUB_WORKSPACE/artifacts/beam-otp-oracle/lib/erlang" \
  --build-evidence "$GITHUB_WORKSPACE/artifacts/beam-source-build-evidence.json" \
  --approved-source-plan-sha256 "$CONTROL_WAIT_SOURCE_PLAN_SHA256" \
  --output "$GITHUB_WORKSPACE/artifacts/distribution-control-wait323-original"
```

No retry or automatic budget relaxation exists. Failure to establish stable target busy is an honest prerequisite failure, not a passing language check. No SDK execution, original/CLR timing or raw-wire equality, native equivalent of the CLR ticket-turn mechanism, general distribution completion or physical memory parity is claimed.
