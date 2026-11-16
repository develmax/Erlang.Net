# Part 1357 — Share literal case results

A case on a call can dispatch atom or small integer patterns directly from X0. The register emitter shares a target when arms return the same literal. An arm that returns its own distinct key can keep the selector in X0. Cases without an explicit fallback retain case_end and its source location.

The planner recognizes local selectors whose terminal paths return exactly the two listed keys, including recursive paths. With that proof it emits one equality test and uses the other arm as fallthrough. Dynamic calls, unproven return values and native replacements retain the checked path. Ordinary tail emission also supports nonreturning Erlang BIFs in these branches.

The fixture verifies six functions against OTP after label normalization. It covers a nested selector, common atom and integer results, exact integer matching, a single arm, an unexpected selector value, recursive return inference and an error raised by a branch. Six previous compiler probes also passed.

The source audit finds a tuple result in a case after a call in 607 modules from the prior differing corpus. It finds one call inside a tuple with otherwise simple operands in 208 modules, including 65 with literal patterns. These are overlapping source-shape counts.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Ninety-five images changed and their Code size gaps narrowed by 1,532 bytes in total. Fifty modules gained 59 normalized function matches with none lost. The peak stack frame shrank in 23 functions and reached OTP's count in 22. The release functions erl_pp:encoding/1 and make:all_or_nothing/0 now match decoded instructions after label normalization. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [erl_pp:encoding/1 instructions](erl-pp-encoding.txt)
- [make:all_or_nothing/0 instructions](make-all-or-nothing.txt)
- [Computed tuple case example](computed-case-example.txt)
- [Focused check](check-literal-case.ps1)
- [Focused output](probe.txt)
- [Two-call case regression](two-call-case-regression.txt)
- [Call tuple sequence regression](call-tuple-sequence-regression.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Closure capture regression](capture-order-regression.txt)
- [Computed case result audit](computed-case-results.txt)
- [Audit script](rank-computed-case-results.eval)
