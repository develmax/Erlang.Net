# Distribution statistics and strict handle bridge, candidate307

Source-only artifact proposal. No Main edits, builds, native/original execution, Git mutation or publication. Root owns isolated build, formatting, existing focused distribution suites and full integration. The reviewed305/306 source overlays are disjoint except the future additive Program registration.

## Original contract and reuse

The frozen six-gap audit C35311... identifies alloc_info/1,alloc_sizes/1,dist_get_stat/1,load_nif/2,memory/0,memory/1 against the exact351 reference union and345 current registrations. This candidate concerns only dist_get_stat. Frozen revision2 F2E1AB... reuses actual independently verified original278 seven-case capture archived in Part279; no redundant workflow is needed. Original raw files, profiles, expected facts and historical guards remain unchanged. Native handles are `{small_connection_id,magic_distribution_entry_ref}`; the current SDK setnode bare-reference return is a concrete existing schema defect.

Native dist.c4489–4529 validates the real handle and current generation, with no controller-only restriction. It returns `{ok,input_attempts,consumed_output_buffers,accounted_queued_bytes}`. Input attempts increment before binary validation; output counts only actual extraction. The third counter is total_qsize, despite its misleading Erlang spec label PendingOutputPackets. dist.c1255–1269 charges actual ERTS buffer/vector metadata plus payload; its sizeof terms vary by implementation/profile. It is neither exact resident heap memory nor merely wire bytes.

Original278 already proves pending and closed zero, owner/controller/third-party reads, malformed handle badarg, stale generation badarg after reopen, malformed input immediate1 followed by teardown zero, additive queue storage and drain counters. LP64 queue sizes238/65808/66056 remain original observations, not asserted C# constants. Positive queued storage is implementation-specific; zero, input/output event counts, lifetime/error contracts and exact managed mass balance remain strict.

## Managed ownership and accounting

HostDistributionBuffer now owns an exact-length immutable BitString reference array and a real readonly value descriptor. Each BitString already owns its exact payload byte array. Charge is owned payload bytes plus the real segment-reference vector storage (`count * IntPtr.Size`) plus `Unsafe.SizeOf<HostDistributionBufferStorage>()`. The descriptor contains the actual readonly collection reference and payload count used by the buffer; no fake native structures or guessed native overhead are allocated. CLR object/array headers, collection wrapper headers and queue/container caches are explicitly excluded. This is a defined actual-storage accounting measure, not CLR resident heap or native physical byte parity.

Admit computes charge before mutating the real queue; drain subtracts the same immutable charge; retire clears it. Existing payload-byte/segment/buffer pressure limits and public HostDistributionSnapshot positional ABI stay unchanged. An internal structural statistics snapshot reads retired queues as zero without requiring a still-live controller, preventing a disposed queue from leaking stale counters during teardown.

## Strict tuple bridge and approved fixture migration

Setnode returns the same typed entry tuple as BeginPending after real admission. Controller adapters validate that tuple via the existing resource lease/owner/current generation checks. Raw input reads the same entry lease while preserving the pinned input-handler check, prevalidation attempt increment and special stale/empty-packet behavior; it does not add an invented early generation rejection. All malformed bare/untyped references remain invalid. The old connected-reference resource kind is retained as storage compatibility metadata, but no new public adoption creates it and adapters do not accept it as a native handle.

Three existing HOST source files have root-approved contract corrections only: ConnectedDistributionHostChecks (returned tuple equals pending, the same pending handle now works for controller get_data), DistributionRegisteredHostChecks (initial and reopened return tuples equal the corresponding pending handles), DistributionMfaSmokeContracts (the return tuple equals pending). Preserve the exact originals in legacy-before and the narrow migration diff. No unrelated assertions are weakened. This corrects an existing SDK schema to the actual native handle protocol; it is not a broad C# host API signature change.

## Installation stages

`overlay/` contains the internal reader/accounting/tuple bridge and new HOST, plus the three legacy fixture migrations. It does not register dist_get_stat by default. Add program-registration.txt once, after verifying current Program parent bytes; do not copy another branch's Program wholesale.

`activation/` is a separate, reviewed follow-up overlay applied only after the base source and meaningful tests pass peer review. Its DistributionMfaSmokeContracts parent is the migrated base overlay, not Main. It adds one actual default MFA registration and its existing registry-smoke route, with provisional registry504/erlang346 until actual exporter/full checks confirm it. No readiness/docs update or API completion is claimed by this unexecuted source package.

New HOST uses genuine runtime actors, real controller adoption, actual raw input routes and actual queue admission/drain/close/reopen. It checks missing/default-independent pending reads, foreign/wrongkind/bare/stale rejection, uint32 connection-id alias behavior, third-party access, prevalidation attempts versus accepted packets, immutable snapshots, two-buffer charge mass balance, no-count empty drain and zero/reset/generation behavior. It avoids asserting a platform-fixed metadata size or native LP64 bytes.

## Dependency decision draft

Reuse the existing repository distribution encoder, actor helper, immutable BitString and BCL arrays/ReadOnlyCollection. Reuse installed .NET System.Runtime.CompilerServices.Unsafe.SizeOf for the real value descriptor size; it adds no package or production BEAM dependency and follows the repository's existing .NET MIT primitive reuse decision. ArrayPool/cached-capacity lists were rejected for permanent queue accounting because retained capacity would obscure this precise owned vector charge. No third-party distribution code was copied. The pinned OTP snippets guide contracts only. Root should append this decision to docs/dependency-decisions.md at promotion; no source banners or LICENSE.local publication.

Remaining: isolated compilation/formatting and all old controller/input/distribution/resource/pressure suites, race review, actual registry activation/full checks, and complete distribution/allocator/native/upstream/performance contracts. No general erlang completion or performance claim.
