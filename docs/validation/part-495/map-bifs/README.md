# Map BIF acceptance evidence

Pinned baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

- [Source audit](source-audit.json): complete-map/nonmap branches, exact key relation, returned value identity, errors, guards and the internal node boundary; current production sources match all existing guards.
- [Original comparison](original-comparison.json):194 values,970 stack profiles, whole raw ETF;1158 literal outputs plus6 whole source-bound node protocol outputs. Original VM receipts, disassembly/apply proof, source, BEAM and raw captures are under `original/`. Each acquisition preserves all6427 provider files and settles its owned process.
- [Final affected regression](affected-final.json):103/103, all100 previous BIF groups plus3 new groups. [Focus](focus2.json):3/3.
- [First encoder failure](managed-first.json) and [whole typed diagnostic](typed-diagnostic.log): generic encoding differed only in large-map traversal order. The explicit owning-runtime public encoder corrects observation; all previous helper callers retain their default encoder. Initial raw captures are retained under `first-encoder-failure/`. [Earlier public-encoder replay](managed-native-guards.json) proves all three prepared groups without changing original bytes.
- `build-final.log` preserves a test-integration compile refusal (ValueTask/Task mismatch). `focus1.log` preserves the rejected selection before new tests were moved ahead of catalog construction. Neither is a runtime semantic failure. Both corrections precede final103/103.
- [Acceptance](acceptance.json): local source/contract checkpoint. The readiness report is an explicit composite of parent exact-head9003 and final103 affected tests, totaling9006 unique names. It is not a new uninterrupted local full run. Exact-head continuous CI is a subsequent publication gate, recorded in the pull request with its independently downloaded artifact.

The source-bound protocol replaces only the actual internal node identity, recursively in the reason and complete stack. It returns `is_map` and `term_type` as well. It does not remove frame arguments, file/line or metadata. Native lookup on an interior bitmap node violates the pinned complete-hashmap assertion; no undefined native-pointer behavior is claimed. Valid raw cursors over a complete map are a separate supported protocol.
