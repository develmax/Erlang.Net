# Part 1352 — Emit boolean cases and construct tail-call lists

The register case planner recognizes two unguarded boolean arms when the resolved selector call has a known boolean return. The proof covers lists:keymember/3, lists:member/2 and maps:is_key/2. A tail call can construct a single cons cell from saved operands directly in its argument register.

For a list head tuple, the planner can read a selector-only field directly into X0 after allocation. It leaves the field in the tuple through the entry checks, trims trailing unused reads and keeps the original list available on failure. When the remaining clause matches only nil and the original input is intact, a tuple mismatch goes directly to function_clause.

The focused fixture compares all decoded walk/2 instructions after label normalization. It checks both boolean results, original arguments in function_clause and case_clause for an unproven selector. The capture, complex branch, deferred field, tuple arity, literal cons and call chain regressions passed.

The full Windows OTP 29.1.1 run generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. Twenty-one images changed and all narrowed their Code size gaps, by 662 bytes in total. Eleven functions gained normalized instruction matches, including xmerl_scan:update_attributes1/2. No previous normalized matches were lost. Six functions reduced their peak frame size to the OTP size. The same 411 functions retained delayed allocation. OTP accepted every differing image for loading.

The source audit of the preceding flat tuple entry group found direct call selectors in 33 modules, boolean arms in 17 and constructed tail arguments in 16. Among complex expression sequences, 32 modules have a clause ending in a tail call and 23 have a clause starting with a call binding. These groups can overlap.

- [Focused check](check-boolean-case.ps1)
- [Focused output](probe.txt)
- [Capture regression](capture-order-regression.txt)
- [Complex branch regression](tuple-branch-body-regression.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
- [Tuple arity regression](tuple-head-dispatch-regression.txt)
- [Literal cons regression](cons-literal-prefix-regression.txt)
- [Call chain regression](entry-call-chain-regression.txt)
- [Full corpus](otp-corpus.json)
- [Normalized instruction comparison](normalized-functions.txt)
- [update_attributes1/2 instructions](update-attributes.txt)
- [Peak slot changes](peak-slots.txt)
- [Entry frame preservation](frame-preservation.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
- [Case selector audit](case-selectors.txt)
- [Audit script](rank-case-selectors.eval)
