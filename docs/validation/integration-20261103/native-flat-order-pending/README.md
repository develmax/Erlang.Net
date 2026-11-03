# Existing native flatmap ordering boundary

The pinned OTP witness materializes a genuine native map with a changed binary key and compares it with a map rebuilt from its entries. OTP reports positive order and false numeric/exact equality; the matching candidate reports zero order, true numeric equality and false exact equality. Actual managed `ordsets:is_equal/2` and `is_element/2` also disagree.

The [raw source and observation archive](source-archive.json) preserves the reproduction, provider receipt, candidate output and source-only regression-boundary audit. All recorded source hashes for main099042,20e02 and f3c4 were checked against Git blobs. Old binaries were not executed by that historical audit. The same sorted-ArithmeticKeyEntries dispatch already exists in accepted main, so this is a pre-existing pending contract, not an atomic-selection regression.

No21/21 acceptance is claimed. Physical non-atom flat-key order, lexical atom handling and nested/value comparison need a separate exact-source fix with genuine native observations and full regression. The integration preserves this open gate without silently changing the accepted compatibility classifications.

[Current source classification](source-classification.json) records why these supported native terms require Known differences for is_equal/is_element/is_set: the first two have actual public observations; reverse is_set is explicitly source-derived from the observed strict order and pinned clauses. The ledger now retains6 verified/12 pending/3 known differences. All previous records and the full prior ledger remain preserved.
