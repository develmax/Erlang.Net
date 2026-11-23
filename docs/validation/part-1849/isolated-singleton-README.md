# Private constructor singleton arguments

Validated against frozen Part 1847. Source zip SHA-256 995a138378d1709c7ab635fb33a5b0b54c7fed61edf22db0f50270594e4da00c. Baseline compiler SHA-256 e6506a169a2a55f7cc42ddca38cf20a1debbc6eed2405f36eda0513cdeee48bd. Reference Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. SDK 10.0.400. No external dependency added.

A structural comparison of saved Part 1845 images found no non-Code primary section difference in megaco_tcp_sup. Four functions differ independently. The private sup_spec/2 and worker_spec/3 miss call-entry singleton values. The other bodies also have distinct register, stack, and instruction placement differences.

Pinned beam_ssa_type joins argument types over local call sites and substitutes a literal in simplify_arg when beam_types:get_singleton_value/1 succeeds. This patch extends the compiler's local constant pass to singleton atoms and [] used in side-effect-free tuple and list constructor bodies. It leaves the calling convention, escaped function rejection, differing-call invalidation, export and native function exclusions intact. The new proof is disabled by the existing SSA option gate. The previous atom relay behavior is preserved.

| Result | Baseline | Candidate |
| --- | ---: | ---: |
| Equal | 4 | 4 |
| Different | 10 | 10 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Elapsed seconds | 5.07 | 6.07 |

One normalized function gained exact equality and none lost it. mnesia_kernel_sup:supervisor_spec/1 folds to a literal. megaco_tcp_sup:sup_spec/2 and worker_spec/3 use the inferred nil or atom but still require sibling-construction ordering and register assignment fixes. No whole-image matches changed. Changed chunks are Code and Attr on megaco_tcp_sup and AtU8, Code, ExpT, ImpT, LitT on mnesia_kernel_sup. Full-corpus verification remains for the integrator.

The permanent regression passes the final candidate and fails the frozen baseline at its proof assertion. It also checks multiple local argument shapes, escaping local funs and no_ssa_opt. Pinned ERTS produced equal outputs for nine fixture cases including dynamic, proper and improper list tails. Both reference and generated megaco_tcp_sup and mnesia_kernel_sup loaded in pinned ERTS and returned equal supervisor specs from init([]) after canonicalizing self(). The SDK style check passed. Raw final evidence is under after-final, alongside the before report. Patched source and regressions are in constructor-singletons.patch.

