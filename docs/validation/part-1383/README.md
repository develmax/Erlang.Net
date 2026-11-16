# Part 1383 — Schedule dynamic map construction in registers

The entry constructor planner retains the evaluation order of pure bindings that feed a map or an ordinary tail call. The argument scheduler counts reads of each binding, reuses X registers after their last read and emits the map fields in exact Erlang key order. A single heap check reserves the space for the preceding tuples and cons cells. The terminal map instruction carries the live X register prefix for its own allocation.

This path covers fresh maps with unique constant keys and variable, tuple or list values. The existing map emitter handles duplicate keys, base maps, exact updates and cases that require further heap construction after the map. The focused fixture checks the results of an inline map argument, a directly returned map, a map that shares a nested value with another argument and a duplicate-key map. The first three functions match OTP 29.1.1 after label normalization.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ssh_tcpip_forward_acceptor_sup.beam now matches byte for byte and is included in the permanent installed-release comparison. ELDAPv3 remains Equal with the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Eight modules changed. Nine functions gained normalized instruction matches and none lost one. All ten changed peak frames reached the OTP size. The eight Code size gaps narrowed by a total of 205 bytes. For example, ct_netconfc:create_subscription/6 now matches the OTP instruction sequence after label normalization. The ct_netconfc image still differs in Code, Type and other chunks.

The largest first-difference group remains allocation operands in 1,844 variable-head sequences across 567 modules. The direct-call binding subgroup covers 640 functions across 250 modules. In asn1ct:compile_proc/3, the OTP frame has three slots and the generated frame has ten. The OTP instructions reuse the Options slot for the constructed state and clear the dead File slot before the next call.

The selected compiler checks, direct whole-module check, layout check of the two construction planners and three earlier focused probes passed. The full runtime suite and canonical readiness regeneration remain pending. Publication remains pending in the shared dirty checkout with a read-only Git directory.

- [Full corpus](otp-corpus.json)
- [Repeatability and compiler identity](repeatability.txt)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Sequence audit](sequence-prefixes.txt)
- [Next call and frame instructions](next-call-liveness.txt)
- [Fixture source](map_argument_probe.erl)
- [Focused oracle check](check-map-arguments.ps1)
- [Focused oracle output](probe.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
