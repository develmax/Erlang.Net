# Part 1807

Delay the shared frame until required by a branch and emit a direct local tail call without a frame when its arguments are already available. Keep receive loop retry edges at the label after allocation.

Reuse the entry-frame control-flow proof and compare branch frame placement with the pinned OTP beam_ssa_pre_codegen pass. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 92 changed images. Gained 2 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by -5868 bytes.

All 11 focused tests passed. Each image passed six runtime assertions covering receive, timeout, recursion and process dictionary effects. The short branch in erts_trace_cleaner:loop/4 uses call_only. The broader compiler block was not run for this intermediate build.

Compact the unused slots in each independently allocated frame.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
