# Part 1262 — Lower calls through inline constant module atoms

Use an ordinary external call when a fresh binding has a constant module atom and the call has a literal function atom and simple operands. Keep supported guards before allocation. A literal return or a local tail call with constant operands needs no saved arguments.

The Windows OTP release records `gen/gl.erl` in its line table and abstract forms. The generated wx driver profile now supplies that relative source path. In the standalone rebuild, the number of `gl` functions matching after label normalization increases from 2 to 645. The release has 1,068 functions and the generated image has 1,062. The six absent functions are list comprehension helpers.

The standalone `Dbgi` section now matches byte for byte. The generated `Code` section falls from 114,874 to 83,021 bytes, with 56,777 in the release. The whole image remains different.

The full run generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Only `gl.beam` changed, and the corpus image matches the standalone rebuild. OTP accepted all differing pairs.

- [Probe source](constant_remote_probe.erl)
- [Probe command](check-constant-remote.ps1)
- [Execution result](probe.txt)
- [Normalized gl instruction comparison](gl-functions.txt)
- [Standalone gl section comparison](gl-chunks.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
