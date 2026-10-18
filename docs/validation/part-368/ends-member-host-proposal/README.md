# Queue ends/member cooperative controls proposal

Two guarded test-only replacements extend the existing HOST name and bounded continuation helper. No runtime file changes or additional test names are needed. Existing four core normal/cancel paths and five reverse-entry controls remain. Eight new representative linear paths each run normal and cancel: out_r balancing, drop/drop_r reverse, get/get_r and peek last traversal, member rear/front search. All use public actor/registry APIs and actual observer reductions.

Input256017 is enough for member16 and reverse40cells/reduction to hit a real own slice. Normal results are checked, not only completion; cancellation must unwind physically and leave the runtime usable. Every independent boundary is collected into the existing aggregate. Shared alias implementations avoid duplicate long allocations for identical loops.

This is source-only, unbuilt and unrun. Existing Main tests and prior seals remain unchanged. It does not claim cancellation during a later higher-operation reconstruction phase. Root may apply only when the two base hashes match, style/build/run, and preserve failures as actual evidence.
