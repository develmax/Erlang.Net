# Part 1158 — align the scratch register for single tuple clauses

A single tuple clause uses the first free X register for literal tests before extracting any bindings. The scratch change aligns the tagged probe and leaves the affected release image hashes from part 1157 intact. Both probes match OTP after local label normalization. A wrong literal raises function_clause on both images.

Against part 1156, the final compiler narrows Code gaps in twelve modules and gains three instruction matches, with none lost. The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. OTP prepared all differing images for loading. All three focused tests passed.

- [Single entry audit](../part-1157/single-entry-audit.txt)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
