# Part 1403 — Repack nested tuple fields in wx calls

The compiler uses the same tuple repacking plan for option callbacks and commands. It checks the two inner tuple shapes in registers, extracts fields in guard order for commands and constructs the output tuple in expression order. Commands accept multiple repacked arguments and support both a tail call and a queue call followed by a reply.

The deterministic fixture matches OTP byte for byte. An Erlang queue stub verifies the exact arguments for one value, a range and a mixed options list. The oracle also checks malformed tuple shapes, invalid integer fields and a missing environment. All three selected compiler cases passed with the same compiler assembly used by the corpus.

All 1,366 release images generated. The strict comparison has 378 Equal, 988 Different, zero SourceRejected and zero Unsupported. wxCalendarCtrl and wxDatePickerCtrl are newly equal, and ELDAPv3 remains equal. OTP prepared all 988 differing images for loading.

Two release images changed. Ten functions gained normalized instruction matches and none lost them. The combined Code gap shrank by 1,186 bytes.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Release targets](release-targets.txt)
- [Option and single-date instructions before](options-before.txt) and [after](options-after.txt)
- [Range instructions before](range-before.txt) and [after](range-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-wx-tuple-repack.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Remaining colour and options sources](colour-option-sources.txt) and [instructions](colour-option-instructions.txt)
