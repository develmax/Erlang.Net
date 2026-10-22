# Backing follow-up1

Source-only, unbuilt, no Main edits. Six guarded files are based on Root's integrated generic-backing successor. Root must review and integrate. No complete optimizer/backing certificate is asserted.

## NIF native-to-managed freeze

NifNativeBinaryStorage.FreezeView holds the existing lifetime gate across validity/range checks and native memory copying. A <=64-byte logical binary copies just its bytes. A larger logical binary copies the entire parent allocation and creates a managed retained view at the actual offset. The copied full array is the actual observed backing: reported referenced_byte_size is not a fabricated native count. Multiple graph nodes may independently copy one native allocation; this is explicitly allowed here and needs no global cache.

BitString.FromOwnedBacking takes ownership of that real full array, always marks it refc, and retains the child logical bit length. Internal bitOffset becomes long so a valid child does not require a full parent's byte size times8 to fit int. Logical BitLength remains the existing int model. Physical parent snapshots still cannot exceed Array.MaxLength; that existing managed allocation boundary must not be called unbounded native equivalence. Root can choose a chunked backing later if the full native allocation domain is required.

The lock prevents managed Release/Reallocate/Dispose from freeing or moving the native storage mid-copy. Native code concurrently writing a borrowed raw pointer is not synchronized by this managed gate; existing native caller ownership and synchronization requirements still apply. This does not promise race-free snapshots of illegally mutated storage.

## Binary generators

ComprehensionExecution and ZipGeneratorCursor now call PinForMatch before input.ToArray. This is the actual start-match boundary for generator readers, before their reused byte snapshots are created. The pin affects the original descriptor and retained aliases; snapshot bytes remain immutable after pin. Do not add a later pin only inside ReadGeneratorAsync while keeping a pre-pin snapshot: that would update capacity after rather than before starting the match.

The direct BitPattern entry pin is from prior proposal. Exact optimizer elision of unused/failed patterns remains a separate native/compiler check. Interpreter/compiled generator code currently shares these entrypoints, so review eval_bits behavior separately before claiming full interpreter pin equivalence.

## Writable initializer API and precise compiler plan

BinaryBacking.InitializeWritable(initialBytes) physically allocates exactly that many bytes and makes a fresh writer token with apparent size0. BitString.InitializeWritable exposes an internal zero-logical-length value. No hardcoded output sizes or native expected results enter Runtime.

Pinned source authority: OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5, v3_core.erl1948 initializes binary-comprehension writable capacity256. Exact exported beam_ssa_bc_size.erl is in authority/. Its22-38 explain exact-size optimization vs fallback256;129-149 reject reused/accepted match contexts; calc_size_instr320+ follows private append, consumed generator bits and integer segment sizes; cg_size_calc568+ falls back256 if generated size calculation fails.

Proposed next edits, not silently implemented by this packet:

1. Carry a typed per-expression storage plan for compiled Expr.BinaryComprehension: fallback capacity256, or a proven size-expression with original source references and match-context eligibility. Do not infer it merely from final materialized result length.
2. Evaluate the first generator's source pre-expression once before initializing the builder (v3_core bc_tq PrePre). Compute only proven pure integer size arithmetic; never re-run filters/body/callbacks to predict size. Preserve error timing and side effects.
3. For the source-proven fixed binary generator and fixed-width output case, initial bytes are ceil((generatorBits div consumedBits)*emittedBits/8). Filters/data-dependent widths/nested unknown iteration require fallback unless the pinned SSA size analysis proves more. Reused match contexts specifically invalidate the otherwise attractive formula. A blanket exact final-size allocation changes observable capacity on filtered comprehensions.
4. Replace binary result List<Term> aggregation with an explicit accumulator initialized once, then append each validated body bitstring in source order. Its same-descriptor token chain permits private-style growth without exposing mutable logical terms. Empty iteration still returns an initialized zero-length refc value; do not substitute an ordinary heap empty bitstring merely because there were no body evaluations.
5. Current FromValidatedAppend builds a full temporary result for each append. A streaming internal builder should validate/encode only the suffix once, append to the actual shared buffer and then publish a new immutable logical view, with cooperative charging per bounded work chunk. Repeated first-source segments must be fully evaluated/read before any shared write. Avoid quadratic allocation in large comprehensions.
6. Ordinary recursive loop private-append conversion needs whole-CFG lifetime/alias proof and explicit writable initializer injection. Existing managed function calls are not that proof. Preserve normal generic append until this analysis exists; report differences rather than naming it native private_append.

Sharing/tracing/message/ETF pin transitions remain unimplemented by these six files. Pin before publishing shared writable storage where the pinned source requires it; do not pin every pure read or referenced_byte_size itself. Native supplement captures before ETF are necessary to distinguish construction state from serialization-induced state.

Required tests: NIF root parent65537 and child257 at offset17 retains65537 after native storage disposal; sibling views preserve exact logical bytes; <=64 child copies; gate protects concurrent disposal; parent offset beyond int-bits fits view index; generator pin shrinks only eligible writable storage before snapshots; initializer0/65/256 allocates actual respective capacities while logical size0; first append fits initialized capacity without generic minimum/doubling; pinned initializer falls back to fresh generic append. No such runtime tests were executed by this source-only helper.
