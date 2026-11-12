# Part 1166 — match the Megaco miscellaneous supervisor

The nil entry path emits a zero-argument tail call and constructs the nested fallback tuple in registers. The literal entry path checks the mode before allocation, keeps only the live argument in Y0 and constructs the result from a tagged tuple after a single heap reservation.

The nil probe matches after local label normalization. Both start probes return the expected values. OTP also infers the success value of the local test callee, which changes the probe instructions. The release Megaco module uses a remote call and now matches byte for byte.

The full run generated 1,363 images. Results were 350 Equal, 1,013 Different and three SourceRejected. ELDAPv3 remains Equal. The only status change was megaco_misc_sup.beam. The instructions of megaco_trans_sup:init/1 also gained a match after local label normalization. OTP prepared all differing images for loading. All three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Source audit](source-audit.txt)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Remaining chunk signatures](chunk-signatures.txt)
