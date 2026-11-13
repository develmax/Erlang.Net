# Part 1573 — Prepare arguments before entering a protected call

The C# compiler keeps straight-line argument preparation outside the try handler. It carries the last preparatory call result in X0 and saves only original inputs read by the protected call. Entry clauses can use the same emitter when their patterns establish the required bindings.

In erl_abstract_code:debug_info/4, the core_v1 branch now uses two stack slots, matching OTP. The entry dispatch and tuple-field register assignment still differ. [Disassembly](erl_abstract_code-debug_info.txt).

Three focused compiler tests passed. The OTP VM agreed on six fixture outcomes, including propagation of an error from argument preparation and rethrow of an exception that the catch does not handle. [Tests](focused.txt) · [Fixture](beam_unused_catch_stack.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt).

All 1,366 sources were accepted and generated. The full comparison found 390 Equal and 976 Different. ELDAPv3 matched using the C# ASN.1 generator. The rule changed 22 images, and OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader and Code](loader-and-code.txt).
