# Part 1808

Track which allocation owns each reachable instruction. Drop unused slots from each independently allocated frame and remap the remaining operands and frame releases.

Reuse compiler stack operand decoding and the existing frame-opcode conventions. Compare branch frame sizes against pinned OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 515 changed images. Gained 5 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -17036 bytes.

All 11 focused tests passed. Each image passed six runtime assertions. The fixture passive receive branch shrank from six slots to zero. The processes arm in erts_trace_cleaner:loop/4 uses the one-slot frame observed in OTP. The broader compiler block was not run for this intermediate build.

Remove detached error exits retained by source annotations.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
