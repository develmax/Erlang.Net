# Part 1162 — plan nested constructions after an embedded call

The intermediate planner moves pure tuple and list construction after the last call. Both supervisors reach the OTP frame size. Megaco gains a normalized instruction match. The broad selection also intercepted a previously matching flat tuple path in asn1ct_check, changing its Y-slot order. Part 1163 narrows selection to nested constructions.

The full intermediate run produced 348 Equal, 1,015 Different and three SourceRejected. All three focused tests passed. The normalized release comparison detected one gained and one lost match.

- [Corpus](otp-corpus.json)
- [Delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Flat tuple regression](regression.txt)
- [Focused cases](focused.json)
