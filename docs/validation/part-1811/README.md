# Part 1811

Replace a receive-result stack spill with its incoming X register when the original message and all lower roots are proved initialized. Extend the heap live prefix as needed and use the value-availability proof before replacing reads.

Reuse BEAM result forwarding and heap-root analysis. Compare both receive behavior and frame size against pinned OTP. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 5 changed images. Gained 0 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -16 bytes.

All 11 focused tests passed. Each image passed eight runtime assertions. The passive branch in erts_trace_cleaner:loop/4 allocates no Y slots after forwarding the message. The broader compiler block was not run for this intermediate build.

Avoid the X0-to-X1 copy for receives whose patterns leave X0 intact.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
