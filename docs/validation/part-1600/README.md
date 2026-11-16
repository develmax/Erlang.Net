# Part 1600 — Reuse saved call slots for constructed tuple bindings

The existing call sequence planner accepts a tuple match whose fields are distinct fresh variables and whose operands are ordinary calls with independent inputs. Calls stay in source order. The planner reuses slots as input values expire and keeps the last result in X0 for the returned tuple.

The deterministic three-call fixture now matches OTP 29.1.1 byte for byte. The two-slot frame, register swaps, source line and tuple operands match. Both images return the same term in the OTP VM. Three selected tests passed with a larger .NET stack and with shared compilation and analyzers disabled for the build. [Fixture](beam_constructed_binding.erl) · [Disassembly](fixture-disassembly.txt) · [SHA-256](fixture-sha256.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 pinned release sources were accepted and generated. There were 392 Equal and 974 Different, with ELDAPv3 Equal. The emitted release images were byte-identical to the previous C# corpus run. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt).
