# Part 922 targeted inlining and residual calls

Targeted inline expansion now evaluates computed arguments into fresh bindings in source order. It evaluates arguments that the target ignores and reuses the evaluated binding when a parameter appears more than once. Recursive expansion stops at an active target. After rewriting, reachable residual calls and local fun references keep their definitions, while private inline targets with no remaining reference are removed.

The pinned OTP compiler's legacy inliner represents application arguments as a let binding. The probe confirms argument order and single evaluation through process dictionary effects. The private duplicate target disappears and the identity function remains callable through a local fun reference. [Reference execution and disassembly](reference.txt).

The implementation retains the existing admissible inline head and body rules. Multi-clause functions and unsupported body templates still need lowering.

All three selected compiler tests passed. The old rejection assertion for a computed inline argument was replaced by the runtime regression. The full Windows OTP 29.1.1 run reported 221 Equal, 826 Different, 105 SourceRejected and 214 Unsupported across 1,366 files. Eleven moved from Unsupported to Different and no status regressed. None of the fourteen previous residual call sites is still the first failure. The inliner still reports twenty body template failures and eighteen head failures. [Focused report](focused.json) and [corpus report](otp-corpus.json).
