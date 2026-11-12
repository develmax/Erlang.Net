# Part 1264 — Emit Unicode comprehension helpers and pre-frame list lengths

Extract the existing Unicode comprehension helper when its result flows to a queue call through a constant module atom. Support the one-cell NUL suffix with `allocate_heap` and `put_list` before conversion. Keep the existing recursive result construction and bad generator path.

For a simple constant-module wrapper with a guarded `length/1` binding, evaluate the length before allocating the call frame. Insert the count among the forwarded registers with adjacent swaps and emit the external call.

All six missing `gl` comprehension helpers now match the release after label normalization. Overall, 876 of 1,068 functions match, up from 745. The Code section falls from 79,294 to 71,205 bytes, with 56,777 in OTP. The local function table now has the release length, and Dbgi remains byte-identical.

The final corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `gl.beam` changed, and its bytes match the standalone rebuild. OTP accepted all differing pairs.

- [Probe source](unicode_suffix_probe.erl)
- [Probe command](check-unicode-suffix.ps1)
- [Execution result](probe.txt)
- [Normalized gl function comparison](gl-functions.txt)
- [Six helper matches](gl-helpers.txt)
- [Standalone gl section comparison](gl-chunks.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
