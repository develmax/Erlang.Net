# Part 1803

Use the existing argument liveness proof for a linear framed suffix reached through a prefix with frameless exits. Drop an entry spill only when all uses of its original value are covered.

Reuse the normal BEAM edge and value availability analysis. Compare the resulting argument projection against OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 208 changed images. Gained 13 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -18001 bytes.

All 11 focused tests passed. Each image passed four runtime assertions. ct_ftp:terminate/2 now reads the record field from the incoming X1 as in OTP. The broader compiler block was not run for this intermediate build.

Move terminal error blocks after the success path.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
