# Part 1106 — pass the head of a cons directly into its first call

The general emitter handles a single cons clause, with an optional empty-list return. The first ordinary call consumes prepared operands including the head, its result is matched against a literal, and a local tail call receives the tail. The frame preserves only the tail. A failed literal match reports the actual call result from X0.

The pinned OTP probe matched both functions instruction for instruction. It executed the successful list path, the empty fallback, badmatch with a non-boolean result and function_clause on an improper list. An additional experiment with a local boolean-only callee showed that OTP can specialize the badmatch operand to false. The new path keeps the general operand.

- [Probe source](beam_cons_call_match_probe.erl)
- [Execution and exact instruction assertions](run.eval)
- [OTP and generated instructions](disassembly.txt)

Both check_NMTOKEN functions in xmerl_xsd_type match OTP apart from label numbers. The full corpus produced 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. Code length gaps narrowed in xmerl_xsd_type and httpd_util. OTP accepted all differing images in loader preparation.

- [Release disassembly](xmerl-disassembly.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
