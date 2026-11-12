# Part 1265 — Match type alternatives and tuple entry forwarding

Emit pure type alternatives on one entry register as ordered tests with a common success label. Keep guard failure ahead of frame allocation. A simple tuple tail wrapper can now read a nonfirst tuple argument, reuse its register for the last extracted field and rotate four or more contiguous arguments using the release swap order.

The standalone `gl` image matches 1,024 of 1,068 functions after label normalization, up from 876. The generated Code section falls from 71,205 to 58,751 bytes. The release contains 56,777 bytes. `Dbgi` remains byte-identical.

The probe covers type alternatives, a wrong tuple arity, second-argument extraction and four-register rotation. It caught an intermediate wrong-register extraction. The corrected version reads the same tuple register that passed the shape check.

The final run generated all 1,366 images with 359 Equal, 1,007 Different and no rejected sources. All 78 changed images moved closer to the release Code length, by 15,222 bytes in aggregate. The corpus gl image matches the standalone rebuild. OTP accepted all 1,007 differing pairs.

- [Probe source](type_or_tuple_probe.erl)
- [Probe command](check-type-or-tuple.ps1)
- [Execution result](probe.txt)
- [Normalized gl comparison](gl-functions.txt)
- [Standalone gl sections](gl-chunks.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
