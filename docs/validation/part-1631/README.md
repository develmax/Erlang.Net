# Part 1631 — Reuse monitored receive emission after entry guards

The existing monitor frame can follow a simple type guard or an entry clause whose tests and X-register projections have already run. The guard fails before allocation. The entry clause passes the surviving bindings at their existing X-register indices. The receive continues to use the two-slot monitor and marker frame. Pinned OTP release code and the existing receive planner provide the reference. No dependency was added.

The permanent fixture matches OTP byte for byte for atom/list clause dispatch and a single guarded clause. The OTP VM verified accepted inputs, function_clause on invalid inputs, and that failed guards do not call setup. Three focused checks passed. [Hashes](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The Windows corpus accepted and generated all 1,366 sources. 393 matched byte for byte, including ELDAPv3. 973 differed and the OTP loader accepted all of them. Observer was the only changed release image. Its start_and_wait/1 matched after label normalization, and the Code size gap narrowed by 11 bytes. Its start/1 still differs. [Corpus](otp-corpus.json) · [Observer disassembly](observer-disassembly.txt) · [Loader and size](loader-and-code.txt).

The builds used SDK 10.0.404 through MSBuild. The full test suite and readiness regeneration were outside this run.
