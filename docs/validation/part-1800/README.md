# Part 1800

Follow the result of update_record to its input when the size matches and the field list leaves the tag intact.

Reuse the record proof and the one-based update_record field indexes in pinned OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 26 changed images. Gained 0 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -233 bytes.

All 11 focused tests passed. Each compiler passed 16 runtime assertions. The from_updated/1 instructions match OTP after label normalization. The broader compiler block was not run for this intermediate build.

Use exact literal tests to prove the argument on the equality edge.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
