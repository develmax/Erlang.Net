# Part 1039 short-lived call results

The general emitter evaluates unary call arguments directly into x0. When the final tuple element is an unguarded call, it keeps the result in x0 and marks that register live at the heap test. Earlier elements are still evaluated in source order and saved as needed.

In asn1_db:init/2, the generated frame shrank from five slots to three, the release frame size. The release still uses different argument moves and Y register assignments. Across all differing images, the number of matched functions with the same first allocation size grew from 3121 to 3238. The count with a larger first allocation fell from 33945 to 33828. Both runs still have 1002 modules containing at least one such function. The frame analysis compares the first ordinary allocate per matched function.

The corpus admitted and generated 1363 of 1366 files. All per-file comparison statuses stayed the same. ELDAPv3 is among the 342 exact matches.

A separate pass used erlang:prepare_loading/2 in OTP 29.1.1 to check the 1021 differing images without installing their code or executing on_load. All 1021 corresponding reference images prepared successfully. The loader accepted 43 generated files and returned badfile for 978. The previous generated files gave the same aggregate counts. For example, the loader reported no specific operation found in asn1_db:dbstart/1 both before and after the compiler change. The preparation pass does not verify module execution.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [asn1_db:init/2 disassembly](asn1-db-init.txt)
- [Frame counts](frames.txt)
- [OTP loader baseline](loader-baseline.txt)
- [OTP loader results](loader.txt)
- [Loader analysis script](prepare-loading.eval)
