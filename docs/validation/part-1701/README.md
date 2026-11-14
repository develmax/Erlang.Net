# Part 1701

The emitter recognizes a straight-line wrapper that calls a local function and returns its provable singleton result. Both compiler entry paths now emit the existing register tail call before allocating the frame or interning the redundant result operand. The implementation reuses local result analysis, ordinary call resolution and the register tail emitter. It follows OTP 29.1.1 beam_ssa_codegen handling of frameless local tails. No dependency was added.

The permanent fixture matches the entire deterministic OTP image, including the atom table, code and attributes. The build and three focused tests passed. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_frameless_singleton_call.erl) · [SHA-256](fixture-sha256.txt) · [Tests](focused.txt).

All 1,366 Windows OTP 29.1.1 sources compiled. The 398 equal images, including ELDAPv3 generated from ASN.1, and 968 differing images are byte-for-byte unchanged from Part 1700. The OTP loader accepted all differing images. Full runtime tests and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Comparison](normalized-functions.txt) · [Loader](loader-and-code.txt).

The Part 1700 opcode audit still shows excess moves in 967 differing images and excess jumps in 960. Compare repeated terminal blocks and their branch targets next. [Opcode counts](opcode-baseline.txt).
