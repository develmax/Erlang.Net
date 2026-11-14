# Part 1699

The register value pass folds a local call and terminal return when it removes a redundant move of the call's proven result. It emits call_last if the original sequence deallocates a frame and call_only if it returns directly. The pass keeps the existing call operands and frame size. It follows OTP 29.1.1 beam_z and reuses the existing instruction reader. No dependency was added.

All 1,366 sources were accepted and compiled. 398 images matched, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepted all 968 differing images. Code gaps decreased in 17 changed images and normalized instruction match counts stayed the same. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The OTP VM verified the control wrapper's return value and side effect. The C# wrapper emits call_last with a zero-slot frame, while the OTP wrapper is frameless. All three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_singleton_call_result.erl) · [Disassembly](fixture-check.txt) · [Tests](focused.txt).
