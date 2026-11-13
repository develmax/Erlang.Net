# Part 1608 — Trim released call argument slots

After preparing the final call arguments from a checked tuple, the saved-call planner can move a lone surviving stack value into a released slot. The frame prefix is trimmed before the call. In xmerl_xlate:hook/2, the move, trim and final deallocation now match OTP. Projection order, heap liveness and failure block order still differ. [Disassembly](hook.txt).

Three selected tests passed and OTP VM observations agreed for the matched branch, both fallback inputs and badmatch. The compiler build used a larger .NET stack with shared compilation and analyzers disabled. [Tests](focused.txt) · [Outcomes](outcomes.txt).

The pinned OTP 29.1.1 corpus accepted and generated all 1,366 sources. There were 392 Equal and 974 Different. ELDAPv3 remained Equal. Only xmerl_xlate changed. Its Code section is now 276 bytes, the same size as the release section. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt).
