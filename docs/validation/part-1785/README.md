# Part 1785

Move the second pair extraction after a type test on the first field when the failed guard does not read the second field. Keep the original initialized tuple in an aliased register on the failure path.

Follow pinned OTP 29.1.1 beam_ssa_opt sink scheduling. Reuse branch liveness and preserve GC root initialization. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 60 changed images. Gained 1 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by 0 bytes.

All 11 focused tests passed. Each compiler passed 17 fixture assertions. The tuple_case/2 instructions match OTP after normalization. The broader compiler block was not run for this intermediate build.

Remove the duplicate entry stack store that remains in the retained-input path.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
