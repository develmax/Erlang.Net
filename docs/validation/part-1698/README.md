# Part 1698

The register value pass recognizes the result of a local call when every successful source clause produces the same constant. It uses the same constant reader as the existing call result emitter. Modules with on_load and native declarations retain the existing call behavior. The pass also tracks exact equality on the failure edge of an exact inequality test. No dependency was added. OTP beam_jump uses singleton call result facts to remove redundant moves. OTP beam_z then converts some equivalent call and return sequences to tail calls.

All 1,366 Windows OTP 29.1.1 sources compiled. 398 images matched, including ELDAPv3 from the C# ASN.1 generator. All 968 differing images passed the OTP loader. 17 images changed and their Code gaps shrank. The number of matching normalized functions stayed the same. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The OTP VM verified both return paths and the local call side effect. The fixture still differs in the tail call and guard layout. The build had zero warnings and all three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_singleton_call_result.erl) · [Disassembly](fixture-check.txt) · [Tests](focused.txt).
