# Native API compatibility work after Part435

Pinned development oracle: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
Production remains our .NET runtime. The user authorized commit and publication of the completed scope through Part436 on2026-10-22. Later prepared proposals remain separate until accepted.

All four source-native modules prepare: `ets`51/51, `erl_ddll`7/7,
`erts_debug`20/20, `erts_internal`71/71. The unchanged original image
selection prepares258/258. These are body/loading counts, not full contracts.
Default MFA708 and complete-contract118 classifications remain unchanged.
Part436 affected regression passes3528/3528 by a read-only composite receipt
audit of324 settled blocks; the first OOM/interruption and retry metadata
aggregation errors remain retained. Focus258/258 and boot79/79 pass separately.
Published commits60b44ddd,4a365af6 and908cde86 are in draft PR1. GitHub run
39818918224 passes build/style and8812/8813: all22 previous failures are
closed. The remaining real CLR dump timeout is addressed by Part439's
separate owned-parent writer; local56/56 halt subprocess scenarios and a
wrong-parent refusal pass. Clean exact-head complete CI is still required.
Part438 DDLL serial rollback passes36 complete original comparisons and194
affected checks locally; concurrent parent visibility remains open and these
production edits remain outside the tools-only Part439 publication.

## Current work

| Module | Current increment | Evidence and acceptance | Remaining work |
|---|---|---|---|
| `erts_debug` | Both unique minima, raw monotonic state read/write, real per-runtime modulo64 counter shared by public unique_integer and both strict trace producers | Original144 outcomes/full profiles captured; actual replay and two-runtime/16-actor controls pass in first bounded block. Focus258/258 in7 immutable blocks passes; affected3528/3528 composite audit passes. | Other `get_internal_state`/`set_internal_state` keys, including actual memory/atom/binary/node diagnostics and execution controls; managed handler/heap layout remains its own backend profile. |
| `ets` | Strict boolean `decentralized_counters` hints and source-ordered last-wins metadata; actual coarse storage reports effective false | Original192 outcomes captured. Initial186/192 exact: six own-PID-bearing stack observations differ only by real VM PID. Original/actual raw retained. Successor source explicitly verifies own PID in the native argument/error-info frame;192 source outcomes and both whole raw/source-produced replay files match. Focus258/258 passes; affected3528/3528 composite audit passes. | Genuine compressed row codec/ownership/census; actual fine-lock/decentralized backend if effective=true is required; unsafe raw-header and physical memory-layout boundaries remain separate. |
| `erts_internal` | Monitored spawn without traditional monitor bits: real connection-only supervision and omission of unsupported MONITOR/DEMONITOR/MONITOR_EXIT wire controls | Original42 complete outcomes/full profiles captured; actual source replay and real two-runtime child-death/node-down/demonitor/abandon controls pass in first bounded block. Focus258/258 in7 immutable blocks passes; affected3528/3528 composite audit passes. | Pending/autoconnect generation-owned output, driver-port distribution controller, pending-node-down retry, unsupported wire capabilities; binary/driver-child allocator attribution and other recorded physical profile boundaries. |
| `erl_ddll` | Genuine nested driver registration rollback when entry.init fails | Local Part438:36/36 complete original outcomes/full profiles, genuine native child exit/cleanup and zero leases;194/194 affected cases. Six production source units remain unpublished. | Atomic parent visibility during linked initialization; other load/unload/reload/monitor partitions and additional platform/native profiles. Adjacent erlang port APIs are tracked separately. |

## Acceptance order

1. Run focused contracts in independent bounded blocks; preserve all earlier
   failures and the interrupted first aggregate without inferring completion.
2. Integrate and review genuine DDLL rollback with actual native fixtures and
   matched original observations. Keep Root as sole shared-code/build/VM owner.
3. Implement real compressed ETS row storage; do not accept a flag while
   retaining an uncompressed value graph or invent physical memory figures.
4. Implement pending distribution output with real kernel admission,
   generation transfer, abort/retirement, and reference ownership.
5. Re-run affected blocks, strict258-image preparation, boot79 and actual
   cleanup on final sources/binaries; preserve exact receipts and hashes.

## Important distinctions

- Creation heir options already pass through the genuine heir-plan parser;
  the older downstream heir refusal is not a reached new/2 limitation.
- Read/write concurrency options are accepted hints. Effective info fields
  describe real storage, not the requested hints. The pinned implementation
  also conditions decentralized counters on its actual fine-lock status.
- A PID-bearing raw stack from two independent VMs cannot have identical PID
  bytes. Source-owned identity predicates and retained native raw observations
  are reported explicitly, never hidden by PID/frame/result normalization.
- Native ERTS opcode vectors, heap pages, allocation headers and physical
  scheduler counts are not fabricated for a managed backend. Their protocol
  and own-state semantics need their own evidence before compatibility credit.
