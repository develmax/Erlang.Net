# Part 1627 — Emit hibernate/3 as a terminal receive call

Terminal receive branches can now release the frame when transferring control to erlang:hibernate/3. This includes a receive with no message clauses and a hibernating timeout continuation. Constant and variable arguments use the existing tail move emitter. A constructed list or tuple uses the existing stack-aware constructor planner. Pinned OTP receive code generation and release disassembly provide the reference. No dependency was added.

The permanent fixture hibernates after a timeout with a dynamic argument list. Its full BEAM image matches OTP 29.1.1. The OTP VM observed the process enter hibernation and exit normally after a wake message. The previous compiler emitted an ordinary call followed by a frame return. Three focused tests passed. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM outcome](outcomes.txt) · [Tests](focused.txt).

The full Windows corpus accepted and generated all 1,366 sources. 393 matched byte for byte and 973 differed. ELDAPv3 was Equal. The only changed release image was erts_trace_cleaner. Its loop/4 timeout now uses call_ext_last, but the allocated frame still has six slots compared with zero in the OTP branch. No whole-function match status changed after label normalization. The OTP loader accepted all 973 differing images. [Corpus](otp-corpus.json) · [Release function](release-disassembly.txt) · [Code sizes and loader](loader-and-code.txt).

Both builds used MSBuild from the installed SDK 10.0.404. The full compiler and runtime suites and readiness regeneration were outside this run. The baseline frame ranking identifies 272 modules in the largest oversized-frame subgroup. [Frame ranking](frame-ranking.txt).
