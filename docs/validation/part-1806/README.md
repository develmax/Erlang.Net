# Part 1806

Attach the source position of a record field to its badrecord exit. Relocate explicit single-line exits with their location and omit a redundant line when the same source is already active.

Reuse record access source metadata and the prior terminal exit placement. Compare both disassembly and reported locations on pinned OTP 29.1.1. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 711 changed images. Gained 113 and lost 3 normalized instruction matches. The sum of absolute Code size differences changed by -21271 bytes.

All 11 focused tests passed. Each image passed four runtime assertions. Previous C# code reported line 5. Current C# and OTP report the field access at line 6. The three lost instruction matches contain unreferenced badrecord blocks whose explicit source lines prevented the current dead-block cleanup. The broader compiler block was still running at publication.

Rank remaining differences in code, register assignments and release metadata.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
