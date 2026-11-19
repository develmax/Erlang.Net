# Literal zero receives, retained aliases and adjacent numeric results

This isolated candidate starts from the immutable Part 1861 source
and preserves the exact compiler across both test-only corrections.
Pinned Windows OTP 29.1.1 supplies the source and execution oracle.
No runtime API, dependency or release build option changes.

The compiler build passes. The first test build exposes fixture
access to private helpers and is preserved. The corrected fixture
reads the existing instruction data instead. An initial numeric
source test creates a Y spill and initialization between producer and
consumer, so is ineligible for the adjacent rule. The direct expression
proves the intended sequence and the prior spilled expression is kept
as a negative control. All twelve selected tests pass out of the
9,809-case catalog after these two test-only corrections.

The fresh 27-module selection reports 4 Equal, 23 Different, 0
Unsupported and 0 SourceRejected in 87.27 seconds. No module status
changes, no complete raw chunk gains or losses. All 23 changed files
prepare in pinned ERTS. Seven of 65 comparable functions gain normalized
instruction equality and none lose it. Ten frames move closer to OTP.

- Literal zero removes 20 explicit wait_timeout instructions from
  19 functions in nine modules. Both positive and dynamic timeouts
  stay covered. Actual native fixture execution preserves selective
  scan and retained mailbox messages.
- Both inet_sctp:send/4 and inet6_sctp:send/4 now use the established
  X1 alias in the early tuple, matching the pinned constructor
  prefix while preserving the spill needed after the call. The
  generated alias fixture gives the same positive and overwritten
  dictionary observations as the native reference.
- The source-derived number result settles the entire decoded
  diameter_session:time32/1 body including raw register types
  and source lines. snmpa:sys_up_time/0, snmpm:sys_up_time/0 and
  timer:system_time/0 gain normalized equality. The actual numeric
  fixture preserves integer values and float badarith.

One cumulative frame-distance increase is inherited from Part 1861.
The exact three-compiler comparison of beam_ssa_opt:ssa_opt_sink/1
shows a three-slot frame in Part 1860 and a two-slot frame in both
Part 1861 and 1862. The current body equals the immediate parent.
Pinned OTP starts with seven slots and trims to two and then one
within a selected path. This separate lifetime difference remains
open. No regression is found in the Part 1862 body.

[Frozen source](source-freeze.json) · [Focused cases](focused.json) ·
[Scoped corpus](pilot.json) · [Function delta](pilot-summary.terms) ·
[Operand and timeout evidence](stage-operand-details.terms) ·
[Frame diagnosis](frame-outlier-raw.terms.gz) · [Native execution](native-fixtures/).

## Full uncached release comparison

The complete four-partition Windows OTP 29.1.1 run confirms 409 Equal,
957 Different, 0 Unsupported and 0 SourceRejected. No file status
changes. All 364 changed generated files prepare in pinned ERTS.
Among 957 comparable changed bodies, 65 gain normalized equality
and one loses it. The Type section of ex_notificationMessage becomes
byte-identical. No raw section loses equality.

The single loss is erl_syntax_lib:annotate_bindings/1. Its pinned
successful result is constrained to the source pattern's known atom
tag, and SSA codegen emits a bare is_tuple followed by test_arity.
The immediate keyfind annotation adds a type operand to the bare
test. A wildcard first field retains the type in the native oracle.
The distinct source shapes require separate provenance.

Frame distance moves closer in 237 bodies and farther in two.
ssa_opt_sink/1 is inherited from Part 1861 as documented above.
core_lint:apply_op/4 remains a separate lifetime triage item.
The exact rank records opcode and frame operand groups for all 957
different files, with no unreadable images.

The four isolated partitions take 594.40 seconds, down from 604.36
seconds on the same inputs by 1.65 percent in this pair. Median
per-image ratios are 0.9762 for 364 changed images and 0.9244 for
1,002 unchanged images. The effective build profiles agree for all
1,366 paths, including identical generated LDAP content at the fresh
physical path. The input check validates 3,016 release files, 265
supplements and 89 ASN.1 outputs. The official checkout stays clean.
The rank takes 285.30 seconds on the current candidate and its
diagnostic cache. The complete managed report remains the preceding
9,802/9,803 run with the original Jaro host deadline failure.

[Full classifications](full-otp-corpus.json) · [Raw preparation](full-loader.terms.gz)
· [Normalized counts](full-normalized-counts.terms) · [Raw lost body](full-loss-erl-syntax-lib.terms)
· [Pinned shape fixture](keyfind-loss-oracle/tuple-tests.terms)
· [Profiles](profile-equivalence.json) · [Inputs](input-verification.json)
· [Timing](full-timing-comparison.json) · [Rank](remaining-causes.md)
· [Overlay transfer receipt](source-transfer.json).

