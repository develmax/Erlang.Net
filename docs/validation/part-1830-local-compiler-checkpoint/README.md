# Part 1830 local compiler checkpoint

This checkpoint saves the working compiler, BEAM fixtures, registered tests and corpus tools of the local fix/local-record-list-tail branch based on 3ea4bc0f. The [staged inputs](staged-inputs.json) identify 120 files. The changes cover checked entry projections, stack ownership, call argument and return placement, type propagation, assembly tables and corpus diagnostics. The three root assembly listings are local diagnostic outputs.

The [test project](build.json) and [corpus tool](corpus-build.stdout) build with .NET SDK 10.0.400. [Seven selected registered cases passed](focused.json), covering checked list returns, adjacent calls, argument movement, owned frames, private atom cases, line filenames and closed literal bindings. The [opcode ranking smoke check](rank.stdout) ran the updated evaluator on an identical fixture pair using pinned OTP 29.1.1.

The full regression, fresh complete corpus and readiness regeneration are pending. This checkpoint preserves work in this branch. Its scope is separate from the published PR 143 and the latest acceptance results recorded in other worktrees.
