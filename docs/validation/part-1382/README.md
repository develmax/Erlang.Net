# Part 1382 — Fold construction sequences before emission

A pure sequence of fresh bindings can fold constants and inline single-use tuple or list constructors into its returned value. The shared construction scheduler plans the remaining objects in X registers, reuses registers after their last read and emits one heap check. If a constructed value is read twice, the planner preserves the existing path so the value can be shared.

The constant call sequence planner also accepts a single nil parameter. The existing entry test runs before the call, so an invalid argument cannot trigger its side effects. After the call, the planner returns the folded result literal.

The oracle verifies frame-free nested construction, preservation of a shared value and the order of the nil check and side effect. Both focused functions match the OTP instructions after label normalization.

The verified Windows OTP 29.1.1 run generated all 1,366 images with 371 Equal, 995 Different and zero SourceRejected. httpc_handler_sup.beam and ssh_lsocket_sup.beam now match byte for byte. They are covered by the installed release check using the pinned source and BEAM resources and their source-relative release profiles. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 995 differing images for loading.

All nine changed images narrowed their Code size gaps by a combined 766 bytes. Ten functions gained normalized matches and none lost one. Thirteen functions reached the OTP peak frame size. The largest first-difference group still has 1,844 variable-head sequences in 567 modules. The construction change brings ssh_tcpip_forward_acceptor_sup.beam to one remaining differing function, start_child/7. Its call argument requires a dynamic map with tuple and list children.

The selected compiler checks, direct whole-module check and three earlier focused probes passed. The layout checker passes for the new construction emitter. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Sequence prefixes](sequence-prefixes.txt)
- [Fixture source](construction_sequence_probe.erl)
- [Focused check](check-construction-sequence.ps1)
- [Focused output](probe.txt)
- [Remaining SSH call instructions](next-start-child.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
