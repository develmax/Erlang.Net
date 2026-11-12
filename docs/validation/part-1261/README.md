# Part 1261 — Initialize only unwritten tagged-cons entry slots

Reserve slots for direct tuple fields before emitting `init_yregs`. Every reserved slot is filled by `get_tuple_element` before the body can call or allocate. Use the existing entry reference analysis to skip unused fields, tail bindings and arguments. Retain every binding when the analysis cannot classify the body.

In `dbg_ieval:eval_zip/5`, the first `init_yregs` now clears ten slots instead of twelve. The release clears one. The focused execution probe verifies that a direct field is omitted from the clear list, that an unused field is not extracted, and that a closure retains the fields it captures.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. The seven changed images moved 42 bytes closer to the OTP Code length in aggregate. There are 32 fewer slot clears across 21 functions. Unused binding elimination is covered by the probe and produces the same corpus images as the intermediate initialization-only run. OTP accepted all 1,007 differing pairs.

The part 1260 baseline contains 32,374 functions across 997 differing images where the first generated allocation clears more slots than the first release allocation. This ranks instruction symptoms across different source shapes. The largest module by count is `gl`, where `get_interface/0` is a declared inline constant helper.

- [Probe source](direct_field_init_probe.erl)
- [Probe command](check-direct-field-init.ps1)
- [Execution result](probe.txt)
- [Debugger release entry comparison](release-entry.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Cleared slot delta](slot-clear-delta.txt)
- [Initial clear baseline ranking](entry-clear-ranking.txt)
- [OTP loader](loader.txt)
