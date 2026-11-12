# Part 1118 — emit a guarded unary BIF directly into a tail-call register

An initial sequence of clauses with variable patterns can test an entry operand in an X register, run a unary GC BIF on it in place and tail-call a local function without allocating a frame. The numeric type established by the guard is attached to the BIF operand. The remaining clauses retain the general path and receive untouched entry registers when the guard fails.

The pinned OTP 29.1.1 probe matched the instruction prefix through both guarded clauses. It exercised negative and positive float conversions and the unmodified fallback.

- [Probe source](beam_guarded_bif_tail_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching guarded instructions](disassembly.txt)

Both floor branches in the release fQuotient/2 match OTP instructions apart from labels. The remaining arithmetic branches still allocate a frame. The full corpus generated 1,363 images, with 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code length gap narrowed in both changed images.

- [Release guarded prefix](fquotient-prefix.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
