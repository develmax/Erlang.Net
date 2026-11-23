# Scope of the release diagnostic

The exact current compiler re-emits the regressed instruction body at its 6653 entry after applying the generated parser's explicit inlining plans. Its private entry range remains 0..805. This narrows the observed failure to the existing bounded plan's body and metadata safety conditions. See [reflected release instructions](reflect-inlined-physical.terms) and [range capture](current-release-inlined.stdout.log).

The synthetic diagnostic exercises the compiler's actual planner against a controlled remap. It proves that a duplicate closure remapping can cause the same untyped, unshared fallback. The equivalent one-remap case is recovered by neutralizing the second lookup, while a genuinely referenced arm retains the untyped fallback. This establishes the defect and qualification of the source correction on the controlled inputs. [Exact compiler diagnostic](synthetic-red.stdout.json).

The actual pre-selector map for the full release was not captured. Reusing the post-emit compiler state and replaying earlier phases in reflection reached a private `ShareReturnBlocks` invalid index. The earlier rejected diagnostic and stdout are preserved in the artifact. A fresh retake must measure the actual parser recovery and test the adjacent bounded selectors. The release status remains the original full run's result.

