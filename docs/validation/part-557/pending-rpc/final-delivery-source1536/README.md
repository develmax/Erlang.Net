# Final delivery source/checkouts draft1536

All four scripts are artifact-only drafts; none was executed by their author. Python AST syntax checks passed. Root reviews and runs them. No W, Git, CI or test processes were changed by preparing these files.

## Run after the actual final1532/1533 build reviews exist

`stage-and-prove.py` arguments:

```text
--workspace W
--git-dir EXPLICIT_G
--artifact-root A
--output A/final-delivery-source1536
--debug-review ACTUAL_FINAL1532_REVIEW_JSON
--release-review ACTUAL_FINAL1533_REVIEW_JSON
```

It copies private index1528 (`fbe24f3576ae9da5dafed50e5559d8f771861b0c`) and stages exactly16 metadata1529 payloads under manifest SHA `8260F55C4931F202393304993EDEF717433117013E3D09CE21509CAA02A8E046`, preserving existing modes. It then proves the equal13,911 final build maps incrementally against raw source proof1522/tree9660 and old1514 map. Unchanged SHA plus unchanged Git blob inherits raw-byte proof; every changed input is directly rehashed and matched to its new Git blob. There is no commit/ref/shared-index/HEAD/workspace write. Publication parent stays ca298.

Actual failed19/21 and99/101 states remain failed history. Their raw source comparisons do not inherit runtime acceptance.

`verify-native-reuse.py` arguments:

```text
--workspace W
--artifact-root A
--debug-review ACTUAL_FINAL1532_REVIEW_JSON
--release-review ACTUAL_FINAL1533_REVIEW_JSON
--output A/final-delivery-source1536/native-reuse.json
```

This performs **no native rerun**. It verifies actual successful1517/1518 process/observations/completion, the exact93 captured source inputs against current A372 plan, all original local execution guards, and all four current normal runtime DLLs per profile against the originally admitted package hashes. Only then does the report retain the old23-case bounded execution scope. A mismatch fails; it is not normalized or waived.

`checkout-and-inspect.py` arguments:

```text
--workspace W
--git-dir EXPLICIT_G
--artifact-root A
--proof A/final-delivery-source1536/source-proof.json
--checkout A/c1537
--output A/final-delivery-source1536
```

Checkout directory must not exist. It materializes the union of final13,911 source paths, all5639 guard paths,16 metadata outputs, all attributes/workflows/Python and root configs through the actual private Git index, with per-command `core.autocrlf=true` and `core.longpaths=true`. It creates no `.git` and does not pretend this partial dependency checkout is a whole-repository build.

Strict checks remain470 raw snapshot rows,93 declared-mode inputs under exact A372 source-plan bytes,23 case names and REAL `git ls-files` Pending membership4820 with explicit Git/index/work-tree environment. It extracts the two exact current workflow Python bodies.

The5639 mutation-guard raw hashes are **not** asserted as blanket Windows transport contracts. Expected LF/CRLF differences (previously135) are separately inventoried and must all be line-ending-only. The Linux-only nif-entry-original workflow is checked against its exact Git LF blob; its Windows CRLF copy remains explicitly outside that raw consumer profile. Historical1497 failure and1502 inventory are untouched.

`run-admissions.py` arguments:

```text
--checkout-review A/final-delivery-source1536/checkout-review.json
--artifact-root A
--output A/c1537-admissions
```

This1415-derived runner executes all three independent passive commands even if one fails. It retains actual owned-process receipts and requires all13 focused reports, MFA/native and Resource reports plus the already-proved REAL Pending check:16 passive admissions. It runs no SDK/product VM/native code. Failure is aggregated, never masked.

Source publication/commit, current-head CI/review and merge remain root-owned subsequent gates. Native23 reuse and source admissions do not establish whole statistics, APIs, performance or current-head CI compatibility.
