# Part 1607 — Use tuple field registers in call argument construction

Tuple entry checks use is_nil for empty list fields. The saved-call planner can construct call arguments directly from projected tuple fields in X registers. In xmerl_xlate:hook/2, this reduces the initial frame from six slots to two, matching OTP. Heap-check scheduling, frame trimming and failure block order still differ.

Three focused checks passed. The OTP VM agreed on the matched branch, two fallback inputs and the badmatch reason. [Disassembly](hook.txt) · [Tests](focused.txt) · [Outcomes](outcomes.txt).

The full OTP 29.1.1 run accepted and generated all 1,366 sources, with 392 Equal and 974 Different. ELDAPv3 remains Equal. OTP loaded all 974 differing images. Of the 37 changed images, 36 moved closer to OTP in Code size, with a net reduction of 159 bytes. [Corpus](otp-corpus.json) · [Loader and Code sizes](loader-and-code.txt).
