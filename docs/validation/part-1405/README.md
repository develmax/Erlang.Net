# Part 1405 — Dispatch tuple and record overloads before allocating a frame

The emitter tests integer fields and selects a branch by tuple arity or record tag while arguments are still in registers. A record branch can pass the extracted class directly to the command emitter, including when it checks a protected union of classes. Static commands can also check tuple fields before creating a frame.

The deterministic regression fixture matches OTP byte for byte. The queue stub verifies rectangle and record alternatives, two tuple widths, static tuple arguments and the errors for invalid inputs and a missing environment. All three selected compiler cases passed on the same compiler assembly used for the corpus.

The full run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. wxRegion is newly equal and ELDAPv3 remains equal. OTP prepared all 986 differing images for loading.

Four release images changed. Ten functions gained normalized instruction matches and none lost them. Code gaps narrowed by 1,311 bytes across wxDisplay, wxRegion, wxWindow and wx_misc.

The frame audit found 125 differing modules and 206 functions with mismatched first frames in variable-head sequences that begin by binding a local call and end with a local call. The largest group with the same sequence of middle operations is two pattern matches in 26 modules. The audit counts functions and modules that have frame differences, so the group requires a separate check of the instructions for each operation.

- [Full corpus](otp-corpus.json) and [status changes](status-changes.txt)
- [Release targets](release-targets.txt)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-wx-tuple-overload.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Frame audit](frame-audit.txt) and [audit script](frame-audit.escript)
