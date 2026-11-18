# Part 1826 — Keep list allocation roots in frameless leaf functions

A leaf with a plain entry stack allocation, a list construction and
`test_heap` can now use X registers for its former Y slots. Backwards
control-flow liveness determines the root prefix after coloring. A
forward intersection over all predecessor states verifies that every
X register in that prefix is initialized. Earlier heap checks prune
the state. The original frame is retained when any register in the
prefix could be uninitialized.

Pinned OTP `beam_ssa_codegen:cg_alloc` emits `test_heap` for a function
without a stack allocation. Its `beam_validator:test_heap` verifies the
initialized live prefix before pruning X registers. The C# implementation
extends the existing leaf register pass. Tuple construction, combined
stack and heap allocation, and call-bearing functions retain their
existing lowering.

The permanent regression covers the two record clauses in the
minimized `diameter_callback:handle_answer/4` shape. It requires a heap
check with the input root live and no stack allocation. The prior
call-bearing frame control remains in the same test suite. Pinned ERTS
executed five controls each against the official fixture and the two
C# images, covering an empty list, a proper list, an improper list,
a function clause error and collection while retaining compound terms.
The release, before and after images of `diameter_callback` and
`argparse` passed two behavior checks each.

The 23-module representative set retained all five byte matches and
all 18 differing statuses. Three generated images changed. The full
uncached corpus used the Part 1823 inputs, the Part 1824 module selector
and four isolated processes. The 444-second run covered all 1,366 images.
It retained 400 Equal and 966 Different, with zero Unsupported,
zero SourceRejected and zero gained or lost byte matches.

ERTS admitted all 57 changed images. Of the 95 functions that lost a
frame, 89 also have no frame in pinned OTP. Six generated functions
have no matching name and arity in the reference. No removed frame
had an OTP frame counterpart. Six more normalized instruction sequences
match and none were lost. The combined stack/heap group and duplicate
projections across clause failure remain open.

[Corpus](otp-corpus.json) · [Summary](results.json) ·
[Changed images](changed-images.json) · [Loader](loader.log) ·
[Execution](execution.log) · [Normalized delta](normalized-delta.terms) ·
[Frame removals](frame-removals.terms).

The final source uses the existing compact live-operand width helper.
Its 23-module retake emitted the same 18 differing images byte for byte
and retained all five byte matches. The full run used compiler DLL SHA-256
recorded in the summary. The final representative build used
`477FD1926466DAB91BB677EA4789899768A7F16EAA3F4E0C7A9316D25D3C8F28`.

The next diagnostic scan found repeated tuple projections around failure
labels in 429 modules, 1,348 functions and 3,440 candidate edges. The scan
counts patterns. Each planned deletion still needs a predecessor and
register-preservation proof. [Scan](scan-duplicate-projections.terms).
Local compiler and tooling blocks passed 3,835 of 3,836 and 1,174 of
1,178. The original Jaro resource test timed out on the candidate and the
previous compiler. The local test executes a retained OTP string BEAM.
The other four cases passed their focused retakes. Three halt checks
needed the conventional Debug halt probe. The identifier test's first
PID depth-two comparison returned an undefined reference count where the
retained outcome contains 1. Its isolated retake matched. The hosted compiler report passed all 9,746 cases. Readiness was regenerated from the verified handoff. [Hosted receipt](hosted-receipt.json). The original reports and their retakes are
kept in this directory.
