# Writable binary backing proposal

Source-only, unbuilt and not integrated. Root owns integration. This closes a concrete storage-model gap; it is not a complete binary API or optimizer certificate.

Authority is OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. Exact exported sources are adjacent in `../binary-primitives369-source-audit1/authority`. `erl_bits.c` append1725/not_writable1852/private1928/init2021/pin2051, `erl_bits.h` GET376/PIN414, and `erl_bif_binary.c` referenced_byte_size2508 distinguish logical size, actual allocation, and writable state. The last source is in `../binary-search369-proposal1/authority`.

## Implemented source protocol

- BinaryBacking owns an actual observed byte array, a stable descriptor, explicit heap/refc kind, apparent byte size, and a writer token. Every new backing/reallocation/shrink is registered with ObservedPayloadStorage. No reported capacity is invented.
- BitString retains immutable logical offset/length. A large slice retains the descriptor; a small slice copies. Refc stays refc even after pin shrinks below64 bytes. Heap partial referenced size is floor(bits/8), whereas erlang:byte_size remains ceiling.
- Generic append validates/materializes the complete result first using existing BitConstruction. Zero appended bits return the original prefix. A valid current token allows sharing/growing its descriptor and transfers ownership to a fresh token. A consumed token, fixed source, or sliced source allocates new writable backing. Old aliases follow moved backing, retain old length, and read the same prefix bits.
- New generic backing allocates max(256,GROW(ceil(resultBits/8))); further growth uses GROW only. GROW doubles through16MiB and uses1.2 above. Allocation/domain limits remain the CLR array/int model, not a claim of native64-bit size equivalence.
- Match entry pins: clears writer eligibility and shrinks only when spare>=8; aliases see the same new buffer. Pinning never occurs merely for referenced_byte_size or a logical ByteSpan read. Partial-bit logical reads normalize/mask their own final byte, so append into unused bits does not change the old logical value.
- Compiler hookup is compiled first syntactically dynamic whole-binary segment only. Literal normal construction and interpreter eval_bits remain unchanged. BitConstruction still evaluates/validates/encodes before token consumption, including repeated source segments; no old value is destructively resized.

## Exact edit seams and unfinished applicability

1. `BitExpressionBindings.Evaluate` contains the safe generic hookup after `BitConstruction.Create`. This deliberately uses an extra exact-sized temporary allocation; optimize only after preserving evaluation/error order. It cannot model native allocation failure ordering exactly.
2. `BitPattern.MatchCore/Async` pins before snapshot extraction. Generator entry is NOT wired by this proposal: callers may snapshot bytes before `ReadGenerator/Async`; move pin to the snapshot-producing entry, not after a stale snapshot. Inspect compiled matching optimization before asserting every failed match pins identically.
3. Private append requires whole-CFG unique/live analysis, writable-origin chain, and no repeated first source in later segments: pinned beam_ssa_alias876/1001, beam_ssa_destructive_update55/858. Do not infer uniqueness from local textual last-use. Loops recognized there start with init_writable256; ordinary default init is1024. Native first append of200 bytes can fit private256 but generic allocation is400; this source successor does not hide that discrepancy.
4. Binary comprehensions currently aggregate exact result chunks in ComprehensionExecution. Port pinned v3_core/beam_ssa_bc_size sizing and init-writable lowering separately; a blanket256 or1024 policy is insufficient. Attributes/guards remain fresh construction.
5. Message copying, ETF serialization, tracing, NIF inspection and other writable-sharing operations need explicit pin/ownership source audit. A managed lock and token serialize competing appends but are not a substitute for OTP process-sharing transitions. No full lifetime claim until these paths are connected and tested.
6. This adds Terms friendship for Compiler to keep storage operations internal. Every replacement has a Main-base hash. Root must compose method-level with any concurrent BitString changes, not overwrite an unmatched file.

## Required focused controls before integration acceptance

Logical prefix unchanged after growth; old and current referenced size both follow actual allocation; old consumed-token append allocates independently; repeated-source construction reads old source twice; partial-bit append preserves old logical padding; zero append returns same instance; two concurrent append attempts yield one winning shared descriptor and independent fallback; concurrent pin/append gives an immutable coherent result; refc pin below64 retains refc floor distinction; small slices copy/large slices retain; allocation observations correspond to real buffers and obsolete buffers retire after all escaped spans/readers release them. Add direct public binary:referenced_byte_size calls, not private-field-only checks.

Compare native dynamic append, escaped/private loops, comprehension, pin, join/replace retained views using the independent new-only backing supplement. Capture numeric observations before sending/ETF. No expected capacities come from managed results. A passing bounded cohort does not establish missing optimizer/sharing domains.
