# Part 1136 — keep ordinary call results in registers through terminal cases

The entry emitter keeps the result of an ordinary call in X0 for a two-branch case that tests a literal or uses a unary type guard. It saves only the inputs used in the branches. A branch can return a constant, a saved argument or the selector, or tail-call another ordinary function.

The status/3 probe matches OTP 29.1.1 apart from numeric labels. In lookup/3, the frame, argument moves and tail call match. OTP attaches an atom-or-tuple type to the lists:keyfind/3 result, which the new guard path still emits untyped. The probes exercise matches and fallbacks.

- [Probe source](beam_general_catch_probe.erl)
- [Oracle assertions](run.eval)
- [Probe disassembly](disassembly.txt)

The full run generated 1,363 images. Results were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. The Code size gap narrowed in all 74 changed images. After normalizing local labels, 59 release functions gained instruction matches and 0 lost a match.

- [Changed images](changed-images.txt)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Loader results](loader.txt)
