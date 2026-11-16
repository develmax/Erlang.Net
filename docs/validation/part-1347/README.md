# Part 1347 — Match literal list heads before the stack frame

The C# emitter handles leading clauses that match distinct atom or integer list heads and end in a return or an ordinary tail call. It tests the list and extracts the required parts in X registers. Multiple heads share an ordered select_val table. The original inputs stay available to the remaining clauses, which use the existing body emitter.

The focused comparison matches the route/1 and keep/2 instructions through their fallback labels after label normalization. It also checks an improper list tail, reuse of the original input in the fallback, a tail that starts in its final argument register and a conversion failure. The mixed cons, entry call chain and inline constructor regressions passed on the final build.

The full Windows OTP 29.1.1 corpus generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. Fifteen images changed. Seven entry instruction sequences gained matches through the first allocation boundary after label and closure ID normalization, including xmerl_xsd_type:check_date/1. Whole-function match counts stayed the same in the changed images. The Code size gap narrowed in 14 modules and widened by two bytes in diameter_make, for a net reduction of 271 bytes. OTP accepted all 1,001 differing images for loading.

The baseline audit ranks the two OTP instructions after the initial nonempty-list test in functions where OTP allocates later and our code allocates at entry. The most common pair is get_list followed by is_tuple, in 763 functions across 298 modules. The next group is allocate followed by move, in 749 functions across 271 modules. The groups may overlap by module.

- [Focused check](check-cons-literal-prefix.ps1)
- [Focused output](probe.txt)
- [Mixed cons regression](mixed-cons-prefix-regression.txt)
- [Entry call chain regression](entry-call-chain-regression.txt)
- [Inline constructor regression](constructor-tail-regression.txt)
- [Full corpus](otp-corpus.json)
- [Normalized entry instructions](normalized-prefixes.txt)
- [Whole-function matches](normalized-functions.txt)
- [check_date/1 entry disassembly](check-date-entry.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
- [Baseline cons audit](cons-continuations-baseline.txt)
- [Audit script](rank-cons-continuations.eval)
