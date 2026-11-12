# Timer backends

Current publication and clean-tree acceptance are recorded in [Part578](validation/part-578/README.md). Timer constants have separate domain owners; the scoped main tree builds without temporary exclusions. [Part565](validation/part-565/README.md) remains immutable historical evidence.

The pinned baseline is Erlang/OTP 29.1.1, commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`. All 36 `timer` exports have a C#
implementation in `TimerLibrary`. The original `timer.erl` and `timer.beam` are
retained unchanged. This user-requested port does not change other modules'
backend policies.

## Selecting an implementation

Use `RuntimeModuleSelection.Managed("timer")` for the C# implementation. It is
installed only when selected. The default registry does not gain timer-library
exports. Its diagnostic snapshot records the managed assembly and MVID.

Use `RuntimeModuleSelection.Beam("timer", originalBytes, dependencies)` for
the original implementation, with explicitly selected dependencies `erlang`,
`ets`, `gen_server` and `supervisor` and their own closures. The standard library
selection/manifest machinery records the image hash and rejects missing imports.
An application can also explicitly publish the pinned prepared image into its
initialized runtime; the focused comparison tool exercises this loading route.
There is no silent fallback between implementations and no live state migration.

Both alternatives expose the same Erlang MFA API. `TimerLibrary.Invoke` is also
available to C# hosts. Server-backed operations require the normal OTP server
dependencies and a running `kernel_sup`; the library does not replace kernel
boot. Arithmetic helpers, `tc`, `sleep` and local `send_after` retain their
source-specific dependency behavior.

## Implemented behavior

The C# port includes fun/MFA overloads, the four opaque timer-reference tags,
instant sends, native local sends, supervised lazy startup, server callbacks,
ETS timer ownership, cancel/flush, registered names, exit/kill, interval monitors,
overlapping actions and completion-waiting repeated actions. Interval deadlines
advance from the previous deadline, including catch-up; they do not drift from
callback completion. Cancelling a repeated timer stops future scheduling without
killing an already running action. `sleep` chunks values above the receive limit.
Numeric helpers preserve integer/float arithmetic, operand order and errors.

Worker closures are rebound with runtime-visible copied environments. They do
not retain captured terms from a retired server turn. Native runtime timer,
message and monitor services remain shared primitives, not a fallback library.

## Focused acceptance

See [Part565](validation/part-565/README.md) for exact results, source/image and
assembly hashes, failed attempts and command receipts. The permanent validator
compares complete returned terms and exception stacks with the admitted official
OTP provider. It checks every registered export and its actual backend owner.

Only unavoidable nondeterministic observations have explicit projections: successful
timer references are checked as references and cancelled; invalid-time-unit
exceptions check a nonnegative measured duration and replace that duration with
a marker; three upstream testcase return values are fresh local references and
are classified as references. Stack frames and source locations are not removed.
Behavioral scenarios assert delivery, cancellation, process exits and overlap.

The unchanged `timer_simple_SUITE` is replayed testcase by testcase, excluding
`timer_perf`, which is a load benchmark. The separate randomized `timer_SUITE`
load test is retained as a reference, not reported as executed. The entire
repository regression is intentionally excluded by the user's focused-check
instruction. Finite, baseline-bound Windows observations do not certify universal
scheduler timing, distributed-node behavior, physical resource limits or all
operating systems. No performance advantage or universal 100% certification is
inferred from corpus counts.
