# Part593 — node/dist reference inventory

Implemented the native `erts_debug:get_internal_state(node_and_dist_references)`
query and its `{node_and_dist_references, blocked}` partition against pinned
OTP29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). Original Erlang/BEAM
assets and startup backend selection are preserved; the module still has98
exports (27 library and71 shared native exports). This adds a native query
partition, not an exported `erts_debug:node_and_dist_references/0` function.

## Final focused results

| Check | Result |
| --- | --- |
| Clean scoped build, both validator projects | Passed;0 warnings/errors |
| Managed primary and scenarios |262/262 and59/59 |
| Original Erlang/BEAM primary and scenarios |262/262 and59/59 |
| Physical multi-scheduling and root-owner checks |13/13 |
| Explicit connected selection |43/43, partial prebuilt harness |
| SDK C# style |0 files need formatting |

See `oracle-final/*-report.json`, `physical.json`, `connected-connected.json`,
`work/final-build.log` and `work/style-check-final.log`. `sources.json` binds
all changed source bytes to the tested scoped snapshot. `assemblies.json`
records the scoped assemblies and the reused connected harness identity.
No full regression, validate.ps1, canonical readiness regeneration, hosted CI,
PR creation, workflow dispatch or package publication was requested or run.
The connected harness was copied from the retained Part592 harness, then its
runtime/terms/serialization/stdlib bundle was replaced with matching scoped
assemblies; this is explicitly not a fresh whole tests-project build.

## Implemented behavior

- Disabled internal-state access still raises undef. The atomic query traps
  through the selected real erts_internal helper. The tuple requires a committed
  all-scheduler block: block_normal alone gives badarg, as the C function returns
  -1 for a normal-only block and the native guard requires a positive value.
- The scan holds the real actor/thread-progress barrier. Reads of heap identifiers
  occur outside the runtime monitor. ETS read leases, persistent table/arena
  readers and exact recipient operations protect the scanned payloads.
- Produce `{{node_references,NodeRows},{dist_references,DistRows}}`, grouping
  node generations by `{Name,Creation}` and retaining actual owner/type counts.
  Each row includes its total and the owner/type breakdown. Shared physical
  headers are counted once; equal independently stored identifiers stay separate.
- Scan actual process MSO external headers, dictionary roots, queued message
  roots/fragments, regular/native port data, distributed link/monitor records,
  monitor tags, compiled match programs/tracing views, physical ETS rows/heir
  data, timer payloads, persistent identifier roots and live connection controllers.
  Offheap recipient fragments are signal roots; headers already in a process's
  physical MSO stay heap roots. Current-node system and node-to-dist ownership
  edges complete the table. No CLR heap scan or guessed boot inventory is used.
- Persistent roots are read directly without materializing/importing identifiers.
  Compressed ETS counts its uncompressed external key, not encoded body bytes.
  Repeated snapshots preserve counts and separate two generations of one name.
  A real pending distribution handle is attributed to its process and disappears
  from that owner after process death; death also releases the block.

## Preserved discoveries and comparison boundaries

The first expanded comparison passes262 primary plus52/55 scenarios. Its three
failures are retained in `oracle-1/Managed-report.json`: normal-only admission,
compressed body accounting, and a persistent-term observation combining unrelated
system roots with asynchronously retired literal storage. The first two are
implementation corrections. The permanent persistent test now selects the exact
`{system,persistent_term}` owner and waits boundedly for its real retirement;
it does not force ERTS to share CLR/native arena reclamation timing. The later
corpora add complete badarg stacks, an external compressed key, monitor tag
custody and exact offheap-message signal counts. No stack frames are removed.

Raw node/dist rows contain VM-specific identities, hash-table order and physical
lifetime details. The corpus validates the complete raw row schema/counter sums,
then selects its explicitly created epoch, owner or root type. Physical tests
also exercise actual portable and owned-header modes. Broader distributed
fragmentation/NIF-environment/resource/collector interleavings, other platforms,
full upstream node_container_SUITE and universal module compatibility remain
unverified. This finite receipt is not a100% whole-OTP certificate. Historical
canonical readiness and [Part592](../part-592/README.md) remain unchanged.

## Reproduce only this connected scope

```powershell
pwsh -NoProfile -File tools/Validation/Erlang.ErtsInternalCompatibility/build-scoped.ps1 -Output artifacts/new-node-dist-scoped
pwsh -NoProfile -File tools/validate-erts-internal-original.ps1 -Output artifacts/new-node-dist-oracle -CaptureOnly
dotnet artifacts/new-node-dist-scoped/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/new-node-dist-oracle Managed
dotnet artifacts/new-node-dist-scoped/snapshot/tools/Validation/Erlang.ErtsInternalCompatibility/bin/Debug/net10.0/Erlang.ErtsInternalCompatibility.dll . artifacts/new-node-dist-oracle Beam
dotnet artifacts/new-node-dist-scoped/snapshot/tools/Validation/Erlang.MultiSchedulingCompatibility/bin/Debug/net10.0/Erlang.MultiSchedulingCompatibility.dll artifacts/new-node-dist-physical.json
```

The exact43-case connected command is saved in `connected-command.json`; adjust
only the output/copy directory when recreating its prebuilt-harness preparation.
Reference sources inspected: erts/emulator/beam/{erl_bif_info.c,erl_node_tables.c,
erl_process.c}, emulator/test/{node_container_SUITE,erts_test_utils}.erl. Use
`git -C ../otp show OTP-29.1.1:PATH`; the sibling checkout is read-only.
No additional dependency or production ERTS runtime is introduced. The derivation
retains the original Apache-2.0/Ericsson source notices in the pinned originals.
