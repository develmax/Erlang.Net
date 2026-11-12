# Part 1400 — Reuse stack slots in call sequences with append

The call planner lowers a nested append with a variable or call on the left into an ordinary call to erlang:'++'/2. It reuses a dying stack slot to preserve the previous call result and swaps the released operand into X0 when the next call needs it there. The existing frame trim drops slots before the following call.

The deterministic fixture matches its OTP image in full. The oracle covers a unary BIF tail call, a local two-argument tail call with a saved value, and append with a variable right operand. It checks returned values and badarg against OTP. All three selected compiler tests passed.

All 1,366 release images generated. The comparison has 372 Equal and 994 Different with zero SourceRejected or Unsupported. OTP prepared all 994 differing images for loading. ELDAPv3 still matches using the C# ASN.1 generator.

Seventeen release images changed. Five functions gained normalized instruction matches and none lost them. The Code gap narrowed by 173 bytes across 16 images. The remaining changed image has the same Code length. The frame in diameter_exprecs:fname/2 now matches OTP at one slot, and its normalized instructions match.

The corpus audit found nine differing modules with one differing function after normalizing labels and closure identifiers and excluding module_info. Four of those are wx modules where OTP checks an entry argument in a register before allocating a frame. The audit found no image with just one differing chunk payload.

- [Full corpus](otp-corpus.json)
- [Instruction changes](normalized-functions.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Loader and Code sizes](loader-and-code.txt)
- [Oracle script](check-append-tail.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Function counts](near-matches.txt), [chunk combinations](chunk-groups.txt) and [wx entry differences](wx-first-differences.txt)
