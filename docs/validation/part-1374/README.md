# Part 1374 — Keep list clause failures in entry registers

List clauses can now use a frame-free fallback that calls an Erlang control-transfer BIF. The shared call scheduler places existing values in argument registers and builds nested tuples and lists for the fallback. Eligibility is determined by clause patterns, resolved call target and argument expressions. Local functions with names such as throw keep their ordinary call behavior.

The oracle fixture covers two input lists, a repeated binding, a successful recursive branch, a throw from the list body and the final throw fallback. It also verifies a nested exit reason and a locally defined throw. The updated check/3 and unwrap/2 begin with is_nonempty_list instead of allocating seven and three slots respectively. The terminal exception calls use call_ext_only. Four earlier probes, the selected Docs and release image tests and the direct whole-module check passed.

The initial implementation also admitted tuple-only dispatch and changed a previously matching closure. Its Code placed a tuple extraction before allocate and added an extra move. The final change leaves tuple-only entry planning on its existing path. The whole-module test passed after this correction.

The complete Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 source generator. OTP accepted all 997 differing images for loading. The 44 changed images gained and lost zero normalized function matches. Their Code size gaps narrowed in 43 images and widened by 14 bytes in array.beam. The net reduction was 1,717 bytes. Peak frame size moved closer to OTP in all 52 changed functions across 41 modules and reached the reference size in 37.

In CryptographicMessageSyntaxAlgorithms-2009:check_oid/3, the generated function checks both list inputs before allocating three stack slots where the earlier emitter allocated seven first. OTP also uses three slots. It extracts the second list after allocation and uses typed operands, a different boolean branch layout and raise/3 for throw paths. The remaining nonempty-list first-difference group drops from 3,496 functions across 579 modules to 3,462 across 574. The largest group by module count is now allocation operands in variable-head sequences, with 1,870 functions across 577 modules.

The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Fixture source](control_transfer_probe.erl)
- [Focused check](check-control-transfer.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
