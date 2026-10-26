# Part490 — Boolean and unary arithmetic contracts

Pinned oracle: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. This part closes native `not/1`, `and/2`, `or/2`, `xor/2`, `+/1`, `-/1` and `bnot/1` on supported valid Erlang terms. No production source changes or backend switches are needed. The existing1686 source guards remain intact; add one guard for the previously unguarded operator-name constants (1687 total).

## Complete source partitions

Pinned erl_bif_op.c defines exactly the four Boolean pairs and the two unary cases. CoreModules.Bool accepts only the true/false atoms; eager CLR Boolean operators evaluate both conversions. Every invalid argument raises badarg, even false-and-invalid and true-or-invalid. These native functions are distinct from source short-circuit constructs. Returned atoms are canonical and arguments are unchanged.

Pinned erl_arith.c splus_1 returns its numeric argument itself. ErlangOperatorValues does the same without conversion or allocation. Non-numbers raise badarith. Native unary minus handles small integers, big integers and floats separately: exact integer negation, fresh finite float with inverted sign, and badarith for every other tag. BigInteger subtraction from zero and finite FloatTerm negation implement those partitions, including both zero signs, subnormal floats and the small-to-big boundary. No user callback or mutation occurs.

Native bnot accepts small/big integers and implements infinite signed complement; big.c supplies the big-integer branch. BigInteger complement implements the same mathematical value, `-x-1`, including negative operands. Boxed results are fresh; immediate identity is relative to our own VM representation. Noninteger operands raise badarith. Physical address identity, allocation ceilings, heap/reduction accounting and identical timing are not asserted. These are not whole compiler/language acceptance claims.

source-audit.json binds these source partitions, pinned source hashes, unchanged managed files and every fixture resource. The general argument is required in addition to finite witnesses.

## Full outcomes and execution

Seven independently bounded groups compare605 actual values and3025 complete stack outcomes (3630 whole comparisons), with default and depth0/1/2/8 stack profiles. Cases cover every Boolean pair, invalid arguments in both positions, all guest term categories, foreign PID/port/reference values, local/external functions, improper lists, large integers, boundaries, negative zero, subnormals and extreme finite doubles. No stack or value fields are normalized.

Every function atom goes through the existing process-dictionary opaque barrier, preventing constant folding of the call. The original disassembly contains actual apply instructions. Existing monitored support/worker BEAMs, their identities and ownership rules are reused. Original acquisition checks6427 provider files before/after and settled children. Original raw ETFs are the embedded goldens; managed execution compares complete serialized results and counts. CI reacquires each group's original goldens with explicit equality checks and aggregates independent failures.

Local8913 readiness is explicitly composite: the parent8906 catalog plus seven passing groups. It is not a continuous final-source8913 run. Fresh exact-publication-head CI/full regression and cold consumers remain delivery gates. The initial source-audit guard check correctly stopped on the missing operator-name guard before modifying the ledger; adding that guard resolves the audit preparation issue without changing runtime behavior.

Registry708, API delta0. Full contracts137->144, pending546->539, known25 unchanged. The original559 cohort is26 closed/533 remaining (4.65%), plus six reclassified audits. erlang47 verified/291 pending/13 known of351; other module counts are unchanged. The broad request remains unfinished. LICENSE.local.txt remains excluded.
