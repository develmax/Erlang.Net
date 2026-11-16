# Part 1153 — build tuple arguments without a stack frame

A baseline disassembly audit classified entry frame differences in all 1,015 differing images. The first frame was larger in at least one shared function in 1,006 modules. In 900 modules, at least one shared function used a frame only in the generated image. The categories overlap and do not identify a single common cause.

The compiler now plans nested tuple arguments for ordinary tail calls using live X registers. It keeps values needed by other arguments live until their parallel copies. In the focused probe, the first argument is also forwarded twice, so the compiler constructs the tuple in X1 before swapping it into X0. The disassembly matches OTP after local label normalization, and both images returned the expected term.

The full Windows OTP 29.1.1 run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code size gap narrowed in all 159 changed images. The normalized instruction comparison gained 653 matches and lost none. OTP prepared all differing images for loading, and the three focused tests passed.

- [Frame census](frame-audit.txt)
- [Census program](frame-audit.eval)
- [ASN.1 database before](asn1-db-before.txt)
- [ASN.1 database after](asn1-db-after.txt)
- [Probe disassembly and execution](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
