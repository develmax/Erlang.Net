# Part 1348 — Dispatch tuple arities after one list extraction

The entry planner shares the list extraction and tuple test across leading tuple patterns with distinct arities. It selects the branch by arity and checks a tag only where the source pattern specifies one. The original arguments remain available for fallbacks, including tuple results built directly in registers.

The call chain planner prepares a singleton list in the allocation for an append used as a call argument. It moves the left operand before construction when this preserves both inputs. Values needed after the append stay in the branch frame.

The final fixture matches decoded OTP instructions for collect/1, collect/2 and strip/1 after label normalization. The behavior checks include both tuple shapes, invalid tags and arities, an improper tail and an append failure. The literal cons, mixed cons, call chain and inline constructor regressions passed.

The full Windows OTP 29.1.1 run generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. Seven images changed. Both xmerl_validate:parse_pcdata functions now match decoded instructions after label normalization. The Code size gap narrowed in six modules and widened by 15 bytes in xmerl_lib, for a net reduction of 426 bytes. In xmerl_lib:simplify_attributes/1 the shared arity dispatch matches the release structure, while failure targets, field extraction and stack slots still differ. OTP accepted all differing images for loading.

The baseline audit narrows the remaining entry cases. After extracting a list head and testing it for a tuple, the release checks the arity and reads a field in 345 functions across 170 modules. The next group checks the arity and allocates before extracting fields, in 187 functions across 139 modules. The groups can overlap by module.

Within the first group, the source audit found a flat tuple in at least one clause in 249 functions across 138 modules. Nested tuples appear in 72 functions across 49 modules. A function can contribute to both groups.

- [Focused check](check-tuple-head-dispatch.ps1)
- [Focused output](probe.txt)
- [Literal cons regression](cons-literal-prefix-regression.txt)
- [Mixed cons regression](mixed-cons-prefix-regression.txt)
- [Call chain regression](entry-call-chain-regression.txt)
- [Inline constructor regression](constructor-tail-regression.txt)
- [Full corpus](otp-corpus.json)
- [Normalized instruction comparison](normalized-functions.txt)
- [parse_pcdata release and generated instructions](parse-pcdata.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
- [Baseline tuple head audit](tuple-head-baseline.txt)
- [Audit script](rank-tuple-head-continuations.eval)
- [Source pattern audit](tuple-head-sources.txt)
- [Source audit script](rank-tuple-head-sources.eval)
