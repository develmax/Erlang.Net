# Part 1123 — reuse the checked list value without a new slot

A binding of an inlined checked list now aliases the original value after the match. A single entry check followed by a tuple return or a local tail call with hd can keep its operands in X registers. The tail path uses get_hd for the checked list and a failing hd BIF for a different list. The original match failure and source location are preserved.

The OTP 29.1.1 probe matches the complete tuple-return function. Execution checks cover the list alias in both tail paths, an improper list, badmatch on the checked operand and badarg on the separate head operand.

- [Probe source](beam_nested_inline_list_probe.erl)
- [Execution and instruction assertions](run.eval)
- [Matching tuple-return disassembly](disassembly.txt)

The release yeccpars2_3/7 no longer allocates seven Y slots. It still checks the semantic stack, uses a failing hd BIF on the separate state stack, and moves the head from X7 to X0. OTP proves both inputs nonempty in this function and emits get_hd directly to X0.

The full corpus generated 1,363 images, with 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. All 13 changed parser images have smaller Code gaps. OTP accepted all differing images during loader preparation.

- [Release comparison](yeccpars2-3.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
