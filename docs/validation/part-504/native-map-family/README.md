# Part504: native maps and shared resource ownership

Parts502/503 are merged into main30d294e5c. Merge CI40400648174 is green. The persistent goal remains all24 recorded differences plus remaining API validation; this increment does not close those24 rows.

## Implemented and locally accepted

All15 Windows x64 map callbacks are bound together: is_map108, size109, new110, put111, get112, update113, remove114, seven iterator functions115–121, and from_arrays165. Callback delta+11 is separate from registered Erlang MFAs.

Pending maps retain live child graphs, flat key tuples, cardinality and persistent HAMT paths. Lookup uses current query hashes against stored branches; insertion rehashes an existing live leaf only when the original split algorithm does. Replacement retains the flat stored key but uses the HAMT argument key when its value changes. Equal native-word replacement preserves the original map word. Copies retain topology, snapshot heap binaries through64 bytes and share larger binary backing. Per-environment iterators preserve entry graphs and release their tables on clear/retirement.

A genuine C/Erlang fixture exercises14 protocol groups, including136 stored-key mutation cases and32 temporary replacement/key-word cases, sizes0/1/2/31/32/33/65, binary copy boundaries63/64/65/4096, compound/exact numeric/own-identity keys, aliases, output sentinels and independent environments. Original acquisition and both managed loader routes produce the same complete440639-byte deterministic ETF, SHA256 `F5BAA4479C6C886E1C5534E0F90C5FB496EFA2D7E822395B43401DCEC1C450A2`. Both loaders prove actual DLL presence/unload, ordinary/reviewed policy counts0/1, and environment/buffer restoration. Provider6427 files remain unchanged before/after.

Actual VM term_to_binary/2 also captures the default profile unchanged. Only the two exact encoded atom-key pairs at root.group9.independent.result_map may permute between own VM atom tables; all other bytes/bindings are strict. No ETF rewriting occurs. See [native comparison](native-comparison.json) and [acceptance](acceptance.json).

Actual native word cells now back BEAM Y roots and frame clone/trim/unwind. Receive selections discarded during cancellation/revalidation dispose only their owned snapshots. Async operation admission drains orphan frames. A separate native term payload/vector store traces explicit roots, reclaims unreachable cycles and waits for held roots at retirement. Guest constructors and all root domains are **not connected** to that term store; public physical heap figures/options remain inactive. Dirty CPU/IO statistics now count actual queued/running work; release-source debug_flags ignores its second argument exactly as OTP does. A separate five-case original Linux CI job is prepared; no execution is inferred from preparation.

## Verification and retained failures

The final build has0 warnings/errors. New18 groups pass18/18; final native/term block passes1706/1706. The earlier8136-case affected run passes7997 and records139 child-launch permission failures; only those139 are repeated outside the restricted sandbox and pass139/139. Reports are retained compressed, with original status/error fields. Saved501 constructor,502 controlled retirement and503 lookup original comparisons also pass on the integrated runtime. Tooling records934/935 before correction of an obsolete run_queues rejection assertion; the two statistics controls then pass2/2 with the production runtime unchanged. All9073 current names have composite local coverage, not an uninterrupted final local run. Exact published-head continuous CI remains a delivery gate.

The extended original experiment exposes an OTP access violation at size33 with duplicate keys created by mutation: removal to32 entries violates the native conversion's unique-key precondition. Two independent runs and explicit progress localize it. The final fixture marks only that removal for binary64/65 as not invoked (-2); get/update/put/copy/iteration remain executed. This is an explicit contract boundary, not a passing removal observation. Failed native/decoder/host setup runs remain historical failures; see [preserved inputs and failures](preserved-failures.json).

## Remaining work

The family remains partial: post-return and nested-map-key operations still need saved physical topology, equality and hash integration after materialization into MapTerm. Linux/ARM/32-bit managed family execution is not accepted. The24 known contracts still require guest term producers, roots across generated locals/mailboxes/native calls, collection, heap options, physical accounting and remaining native loader closure. The [24-row plan](known24-plan.json) and [integration boundary](heap-ownership-integration.md) record the gaps. Source guards1772 and catalog9073 do not imply full contract completion.

Current totals:708 registered,180 full,504 pending,24 known; MFA/full deltas0/0. LICENSE.local.txt remains excluded.
