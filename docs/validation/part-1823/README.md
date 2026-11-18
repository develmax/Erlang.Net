# Part 1823 — Branch stack-slot lifetime at the last read

The comparison uses Windows OTP 29.1.1, OTP source commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`, and compiler main
`da54292c8ca96cf606a54c352d628a94c3f31993`. The SDK is 10.0.400 and the
Windows x64 runtime is 10.0.11. The official release ZIP has SHA-256
`337480AF988ECA993C2E98B68850F00FC6724D366346565E38941AC0D1B7C56B`.

## Diagnostic priority

The rows overlap. The counts below come from the corrected full-input
run. All 925 previously emitted differing images had identical SHA-256
hashes in the retake. Their disassembly results were reused. The other
41 were disassembled separately. Both sets together were readable. All
966 differed in at least one opcode sequence.
These are measurement groups. The pass assignments are hypotheses until
isolated changes account for the differences.

| Priority | Signal | Modules | Functions | OTP pass and confidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess stack slots with equal live X roots at `allocate` | 846 | 8,084 | `beam_ssa_pre_codegen` and `beam_trim`. High for the measured operand difference. |
| 1 | Excess stack slots with equal heap and live X roots at `allocate_heap` | 368 | 889 | Same dependency as plain allocation. High for the measured operand difference. |
| 2 | Tuple projection before frame allocation | 538 | 2,508 | `place_frames` and upstream SSA optimization. Medium. |
| 2 | Tuple test before frame allocation | 475 | 1,301 | Frame placement and clause lowering. Medium. |
| 3 | Spill move where the candidate emits `init_yregs` with a different allocation | 704 | 3,519 | `beam_ssa_codegen` after frame and register allocation. Medium because frame changes alter initialization demand. |
| 3 | Heap check where the candidate emits `allocate_heap` | 417 | 1,653 | `beam_ssa_codegen:cg_alloc` after frame placement. Medium. |
| 4 | Expected functions missing from generated code | 453 | 4,085 | Function naming, inlining, dead functions and synthetic function generation. Low until classified. |
| 4 | Value dispatch lowered to a comparison chain | 351 | 1,574 | Match lowering and SSA simplification. Medium for the opcode signal. |
| 5 | Section payloads differ | Code 966, ExpT 948, Attr 939, LocT 930, AtU8 928, Line 927, ImpT 901, Type 877, LitT 712, FunT 705, Dbgi 332, Docs 66, StrT 46, Meta 1 | — | Atom, import, line and type tables use beam_dict and beam_asm after code emission. Isolated metadata causes still need individual checks. |

## Implemented correction

Pinned `beam_ssa_pre_codegen` assigns the last read and the new definition
the same instruction number. Its `overlap_status` expires a range whose
end equals the start of the new range. The C# branch allocator now lets
single-result instructions that read all inputs before writing share a
destination with a source that dies there. Any value live on fallthrough
still interferes. Failure edges that can write before branching and
installed exception handlers retain their existing interference.

The implementation extends the current C# control-flow and liveness
analysis using its existing BCL bit sets. No dependency or copied OTP
implementation was added.

## Local controls

- The permanent fixture fails the whole-module test with the original
  compiler and passes with the candidate. It requires a two-slot frame
  and the head projection's input and output to share a Y register.
- The fixture's true branch, false branch and case-clause error matched
  pinned OTP on the original and candidate C# images.
- In 51 representative release modules, 23 preserved their full-file
  matches. Four of the 28 differing images changed. The candidate made
  no new full-file matches in this subset.
- The four candidate images passed ERTS loader preparation and executed
  assembly/arithmetic, ASCII-boundary and handshake controls on ERTS.
  `dtls_handshake:reassemble_fragments/2` changed from three slots to the
  reference's two. The complete and fragmented handshake cases passed on
  both generated images and the release image.
- The first serial corpus compiled for 1,021 seconds and recorded 397
  Equal, 925 Different and 44 SourceRejected. The 44 failures lacked
  29 generated public_key headers and documentation included by seven
  modules. The raw report retains all messages. Those inputs were
  recreated using the release ASN.1 generator and the pinned source tag.
  This preparation failure is separate from the compiler result.

The corrected baseline covered all 1,366 files in four independent
processes over 451 seconds. It reproduced all part-1821 module statuses.
The first pass used different inputs, so the observed 1,021 versus 451
seconds describes two runs and does not establish controlled speedup.
[Baseline](baseline-corpus.json) · [Section groups](baseline-sections.json) · [Function groups](baseline-rank.terms.gz).

The first candidate completed in 474 seconds with 400 Equal, 966 Different, zero Unsupported and zero SourceRejected. It gained and lost zero full-file matches. ERTS admitted all 202 changed images. A frame comparison found 211 improved functions and one worsened function, 'code_server:insert_dir/2'. The latter was caused by coalescing a move at a branch join. The revised allocator preserves move interference. Both the pinned compiler and the revision allocate four slots in the minimized join fixture. The first candidate allocated three. The positive projection fixture still allocates two, and both fixtures passed their five behavior checks under ERTS.

The revised candidate covered all 1,366 images in 372 seconds. It retained all 400 full-file matches and produced 966 differing images, zero Unsupported and zero SourceRejected. ERTS admitted all 155 changed images. The frame-size gap narrowed in 68 comparable functions and grew in zero. Normalized instruction matches gained zero and lost zero. Disassembly was reused for 136 images whose output hashes matched the first candidate. The revised compiler block passed all 3,836 cases. The Tooling gate passed. Hosted full regression and readiness are pending.

[Revised corpus](candidate-corpus.json) · [Load comparison](loader-changes.log) · [Normalized comparison](normalized-delta-summary.json).

The next frame-placement samples include "erl_boot_server:valid_option/1", where OTP emits a frameless tuple test and boolean BIF, and "logger_olp:do_check_opts/1", where OTP projects and dispatches the tuple before allocating. The second also depends on clause lowering. Check these shapes independently before changing frame placement. [First candidate](first-candidate/candidate-status.json) · [Frame analysis](first-candidate/normalized-delta-summary.json) · [Representative retake](representative-frames.txt).
