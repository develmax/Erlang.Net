# Part 1789

Replace an entry argument store with frame initialization when the lifetime analysis proves that the copied value has no reads. Allow frame and X-root compaction to follow.

Reuse stack-copy lifetime analysis. Preserve initialized Y slots for collection before their first real write. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 365 changed images. Gained 6 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -103 bytes.

All 11 focused tests passed. Each compiler passed 11 fixture assertions. The init/1 instructions match OTP after label normalization and the frame shrank from two slots to one. The broader compiler block was not run for this intermediate build.

Store closure results directly in Y registers after proving the temporary X register is dead.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
