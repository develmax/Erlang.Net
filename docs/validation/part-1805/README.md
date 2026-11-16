# Part 1805

Extend the live X prefix through a heap check when the existing dataflow proof can replace every read of the saved call result. The construction may use only other values.

Reuse the saved-call-result control-flow proof and compare the live prefix with OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 89 changed images. Gained 4 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -744 bytes.

All 11 focused tests passed. Each image passed four runtime assertions. In ct_ftp:terminate/2 all instructions and registers match after label normalization except the line associated with badrecord. The broader compiler block was not run for this intermediate build.

Carry the record access site to the generic badrecord instruction.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
