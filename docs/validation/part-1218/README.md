# Part 1218 — reuse the line marker from a computed length operand

Argument lowering reports the source location emitted for a direct length/1 operand. Outer calls use that location to skip a duplicate line while preserving a distinct call location. The fast path also checks Erlang auto-import resolution.

The four instructions from length through function_exported/3 in error_handler:undefined_function/3 match OTP. Code size moved closer in 75 modules and farther in none. All 1,366 images were generated. The Windows OTP 29.1.1 comparison produced 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing files. Three focused tests and the same-line and distinct-line execution checks passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Matching instruction window](error-handler.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Execution probe](probe.txt)
- [Opcode ranking](ranking.txt)
