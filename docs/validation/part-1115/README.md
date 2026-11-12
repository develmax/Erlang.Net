# Part 1115 — reuse an input slot for a nonzero argument register

The two-sibling planner can reuse a last-use input slot even when the second call expects that input in an X register other than X0. It loads the input before overwriting its Y slot with the first result. The X0 case still uses swap. Repeated inputs and inputs required after the second call are excluded.

The OTP 29.1.1 probe matched the complete nonzero/2 instruction list and the execution result. The three-sibling function in the probe remains a separate case for the planner.

- [Probe source](beam_sibling_three_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching disassembly](disassembly.txt)

The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Both changed images have a smaller Code length gap. OTP accepted all differing images in loader preparation.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
