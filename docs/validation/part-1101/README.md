# Part 1101 — reuse expired slots for prepared non-call bindings

The general emitter can reuse a dead slot for unary and binary expressions, tuples, lists, record access and constant bindings with prepared operands. It evaluates a potentially failing expression before overwriting the slot. All aliases must be dead in the continuation. Variable-to-variable bindings retain the previous alias path.

The OTP 29.1.1 probe verifies arithmetic, a tuple, a list, a still-live alias and badarith before a subsequent effect. The first three functions shrink from three slots to two, matching the reference frame sizes.

In memsup, part 1100 put the captured Self value in Y1 instead of Y9. The emitter orders captures by Y slot, so the closure receives Self in X0 and OS in X1 instead of the reference order. The additional argument move before get_memory_usage/1 accounts for the three-byte increase in part 1100.

- [Memsup capture order](memsup-captures.txt)
- [Probe source](binding_liveness_probe.erl)
- [Execution assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)

The full corpus generated 1,363 BEAM files. Of the 1,366 reference files, 348 are Equal, 1,015 are Different and three are SourceRejected. ELDAPv3 remains Equal. Since part 1100, 517 generated images changed. The Code length gap narrowed in 500 files and widened in zero. OTP accepted all 1,015 differing images in the loader preparation check.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)

The largest group still starts with OTP move versus generated init_yregs across 813 files. The OTP Core dump for memsup shows that the closure captures the compiler temporary _32 (the OS field) and the source variable Self. This gives a reference for investigating capture ordering through lowering.

- [Opcode ranking](opcode-ranking.txt)
- [Frame size split](frame-size.txt)
- [OTP Core excerpt](memsup-otp-core.txt)
