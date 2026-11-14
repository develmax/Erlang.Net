# Part 1724

Drop saves of incoming arguments to unused trailing stack slots. Eligible leaf functions with zero remaining slots omit allocations and preserve heap checks. Match the boundaries used by OTP `beam_trim` in its call-cost lookahead. No external dependency was added.

The compiler and tests built without warnings. All three targeted tests passed, including whole-image comparison against an OTP fixture for a guarded function with unused entry slots. The Windows OTP-29.1.1 release corpus comparison is in progress. The full runtime suite and readiness regeneration have not been run for this checkpoint.

[Targeted test results](focused.txt)
