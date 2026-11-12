# Part 1250 — Keep atom case selectors out of the stack frame

When a prepared selector is matched against multiple distinct atoms with no fallback, dispatch with `select_val` in X0. Keep the original value in X0 for `case_end` if no atom matches. Lower branch bodies with the existing slot-reuse and continuation rules.

The execution probe compares both successful branches and the unmatched case reason with OTP. Its frame falls from two slots to one. In `asn1ct:check/2`, the frame falls from nine to eight slots, while the release allocates two. The generated function still spills the result of `dbget/2`, repeats record checks, and omits the release line instruction on the outer case failure edge.

The final run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. The 47 changed images contain 59 functions with smaller initial frames. In 46 images the Code section length moved closer to OTP. The `dialyzer_dep` Code section moved 61 bytes farther away despite the smaller `traverse/4` frame. OTP loaded all 1,007 differing pairs.

- [Probe source](atom_case_probe.erl)
- [Probe command](check-atom-case.ps1)
- [Probe result](probe.txt)
- [Probe frame comparison](probe-frame.txt)
- [asn1ct function disassembly](asn1ct-check.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Changed image frames](frame-delta.txt)
- [OTP loader](loader.txt)
