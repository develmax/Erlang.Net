# Constructor destinations and receive-reference lifetimes

The linked source starts from the 44-case Part 1856 fixture retake.
It targets Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5.
The previous uncached complete release result is 408 Equal, 958 Different,
0 Unsupported and 0 SourceRejected. The four independent checks
record their own compiler identities.

| Scope | Classes | Pinned evidence |
| --- | --- | --- |
| Direct post-call tuple store | 5 Equal, 6 Different, with no byte gains or losses. | Whole raw disassembly of mnesia_kernel_sup:init/1 matches. An X0 overwrite releases the temporary after a tuple built with the live call result. All four changed images load and all nine child specifications agree. |
| Returned sibling constructions | 5 Equal, 5 Different, with no byte gains or losses. | Whole raw disassembly of mnesia_kernel_sup:worker_spec/3 matches, with two retained input slots and the original call-result root. The exact-binding and effect-order controls agree in ERTS. |
| Retained entry tuple projection | 2 Equal, 7 Different, with no byte gains or losses. | All five tested beam compiler module/2 bodies gain normalized equality. Their tuple field lifetimes follow pinned SSA sinking and all five actual APIs execute with matching results. |
| Fresh receive marker | 3 Equal, 4 Different, with no byte gains or losses. | Four actual functions gain the full OTP marker signature. Sixteen native fixture executions pass, observer retains whole-file equality, and native code purger and socket registry requests preserve queued unrelated messages. |

The factory routines reuse the existing scheduling, frame tracking,
initialized X prefix and capture analysis. Pinned OTP Core, SSA sink,
code generation and receive analysis determine the transformations
and source option boundaries. The package adds no external dependency
or public runtime MFA. Interprocedural receive-marker propagation
remains a separate dependency of instrument.

Earlier fixture failures are retained with exact build and compiler
identities. The direct tuple test corrects the minimum supported
opcode in its test header. The standalone retained-tuple harness
uses the existing compiler entry that supports no_ssa_opt source
directives. The sibling fixture checks the emitted inverted exact
comparison in its bound-value fallback. The marker receipt retains
its initial unreachable path and required native test dependency.

The linked payload passes all 48 selected tests and compares
64 release paths in 178.93 seconds. The result is 12 Equal,
52 Different, 0 Unsupported and 0 SourceRejected. It gains
mnesia_kernel_sup.beam and retains all eleven previous matches.
All 47 changed images prepare in pinned ERTS. Forty-two of 173
changed functions gain normalized equality, zero lose it, and the
Mnesia Code chunk gains equality. No raw section equality is lost.
An isolated capture tool saves the generated equal image before its
normal comparison. The emitted and installed files both contain
3,528 bytes and have SHA-256
5B28241196989A4F5AC5993E82CB2803D3970E63F053384AB6497087EE462CC6.
Pinned ERTS loads the generated file and its init/1 returns the
same complete observation as the official and preceding files.

Eight comparable frames move closer to the reference and four
move farther away. The latter four are the new marker functions.
They retain the correct marker operations but save reference and
send operands in extra Y slots. Pinned OTP constructs the message
using X0 and X1, binds the marker to the live X0 reference, and
reuses dead frame slots. The recorded frames are 2/3/5, 2/3/5,
3/4/7 for the code purger functions and 2/3/4 for the registry
request, in reference, previous and linked order.

The corrected Part 1855 full managed run remains separate.
The full 1,366-module result for the new payload remains pending.

[Direct tuple evidence](direct-tuple/VERIFIED.md) ·
[Sibling corpus](returned-siblings/corpus-receipt.json) ·
[Sibling behavior](returned-siblings/native-Candidate.terms) ·
[Retained entry tuple](retained-tuple/README.md) ·
[Receive marker](receive-marker/VALIDATION.md) ·
[Interprocedural marker analysis](receive-marker/INTERPROCEDURAL.md).

[Focused result](focused.json) · [Linked pilot](pilot.json) · [Timing](pilot-receipt.json) ·
[Byte changes](pilot-status-changes.json) · [Native admission](pilot-loader-summary.terms) ·
[Normalized counts](pilot-summary-counts.terms) · [Raw sections](pilot-section-delta.json) ·
[Marker frame listings](marker-frame-profiles/) · [Frozen identity](source-freeze.json) ·
[Generated Mnesia hash](capture-identity.json) · [Generated Mnesia execution](native-execution.json).
