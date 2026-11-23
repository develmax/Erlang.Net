# Part 1856 checked entry fixture reconciliation

The exact linked compiler is 4334EA6E7F30F2DF399A11ABCB721CFD2FF30C510382B74356152DB1952A51B9. Its source archive is E3E4222518E462E9FF9581BD7CDF36E0EB3A0EC0988B02D44521A9EAA27A1BD3. The initial linked harness result was 43/44. The existing entry fixture assumed that both checked projections were followed immediately by Allocate. That assumption held for the isolated branch-gated Part 1853 implementation.

The combined compiler now implements the earlier tail-case constructor continuation. Its direct fixture emits IsTuple, TestArity, two X-register projections, SelectValue on the first field, a label and a separate Allocate for each call arm. The pinned reference has that dispatch shape. The tagged case still has two projections before the frame. The alternate-failure and shared-call controls retain their prior frame ownership. Native fixture probes verified both successful arms and unchanged exception classes.

The test-only patch checks SelectValue and its exact input register for the direct constructor fixture. The existing positive and negative Allocate checks remain distinct and unchanged in meaning. The exact linked compiler passes the revised fixture. Frozen Part 1855 fails its placement assertion. Tested probe:finish/1 and /2 variations were deliberately excluded as replacement fixtures because the pinned compiler legitimately retains a frame in front of their shared tail. No compiler bits or reference source were changed by this correction.

Twelve timed pinned ERTS runs passed across four source shapes and three image versions. The uncached nine-release cohort ran in 14.57 seconds. It remained 3 Equal, 6 Different, 0 Unsupported and 0 SourceRejected. Whole-file gain and loss were both zero. Twenty-one normalized bodies changed across four modules. Eight MEGACO methods became normalized-equal and none lost normalized equality. Those methods include tr_MId/2 and tr_Command/2. ct_snmp and pubkey_os_cacerts match their frozen generated files byte for byte. Prior Equal controls stay Equal.

All twelve reference/1855/linked changed-module release images loaded on pinned ERTS with equal export lists. The linked MEGACO production module returned the same values on a verified message and transaction, and the same case_clause for an unknown mId. The identical targeted results ran on all three image versions. Full package validation belongs to the parent worktree.

Patch SHA256 233F9CDFEDED09CE7DAF37DBF087609F57C37EED0A257021EBD7210B7AF666CD. Raw evidence is fixture-operands.log, test-receipt.log, test-green.log, test-red.log, native-fixtures.log, cohort.json, cohort-delta.json, cohort-normalized.terms, cohort-loader.terms and native-megaco.log.

