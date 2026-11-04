# Managed Unicode utility: current-main integration

All20 unicode_util exports are ported to C# and both managed Unicode modules run
without Erlang source, BEAM images or an original VM. Original modules/images and
explicit backend selection remain. The baseline is OTP29.1.1 / Unicode17.0.

The complete implementation and source-bound evidence were first published in
PR117 at71aefd79d. Concurrent gen_statem PR112 used the same Part587 number before
this PR was opened. The exact Unicode evidence tree is preserved byte-for-byte
in unicode-prior-publication.zip and remains available at its originating commit.
The current canonical Unicode work number is588. Main's Part587 receipts remain
unchanged; they are not replaced by the Unicode receipts.

The earlier snapshot records 28 connected groups, 14,801 exact utility observations
per backend, all 1,114,112 codepoints /8,912,896 GC transitions,401,446 conformance
comparisons and 55 managed long cases. Both current Unicode consumer selections
passed 490 +711 +408 comparisons and 29 backing probes. Earlier-assembly stress
and 66,458-case observations retain their own identities inside that snapshot.
No full local regression or global readiness promotion is performed.

This integration preserves gen_statem and refreshes only existing declared source
identities, producer pins and a successor Monitor freeze. The final combined
candidate rebuilds and repeats the 28 connected groups; current-head mandatory CI
and reviews remain independent delivery gates. Reports are retained here with
actual assembly/source identities. Finite evidence is not a universal 100% proof
across arbitrary inputs, platforms and physical ERTS resource states.

## Review and linked error-frame correction

PR117 review identified whole-tail normalization in BitString.ByteSpan for an
unaligned UTF8 input. Cp now reads at most four bytes through the existing bit
accessor and slices the original binary for the tail. The new permanent test
checks every codepoint, original backing capacity, public NFC output and linear
allocation growth. It fails against the old decoder: 4,096/8,192 bytes allocate
12,890,808 /42,151,232 bytes. The fixed decoder measures 3,888,872 /7,889,904 bytes.
The failed control and complete report are retained; no expected value changed.

The linked407 whole-error/stack comparison caught a regression in the incoming
main badfun helper: blindly duplicating the current frame is wrong for a tail
fun application. The interpreter now identifies the existing deallocate/return
tail protocol before rejecting the callee. Non-tail calls keep the suspended
caller frame required by gen_statem; tail calls keep the original single frame.
All 66 exact rows, including configured backtrace-depth profiles, pass again.
The gen_statem module corpus is checked separately to retain its original fix.

The final connected harness adds this allocation/backing case to the previous
28 groups. No full local regression or comparison weakening is introduced.

Final related acceptance:29/29 harness groups pass. The connected gen_statem
comparison also passes74/74 API rows and311 scenarios independently for Managed
and Beam, with zero differences. Current managed table/conformance/long reports
are retained alongside this final assembly snapshot.
