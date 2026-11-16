# Part 1784

Put adjacent fields of the same tuple in X registers before frame allocation. Reuse the input X register for the second field when both successors can discard the tuple.

Reuse entry frame placement and its list projection liveness checks. Compare register allocation against pinned OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 79 changed images. Gained 0 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by 45 bytes.

All 11 focused tests passed. Each compiler passed 17 tuple and list fixture assertions. A guard failure that still returns the input keeps the tuple in its original register. The broader compiler block was not run for this intermediate build.

Place a second tuple extraction after the first field guard when the failure path does not need that extraction.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
