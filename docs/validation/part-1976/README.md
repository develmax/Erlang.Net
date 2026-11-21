# Part 1976 Windows OTP compiler validation

The compiler release gate completed locally. Local repository validation exited 1. Build exit was 0 and validator exit was 1. The managed test report records 10318 passed and 28 failed. Failing cases are `runtime/string/original-jaro-indexed-resource-contract`, `runtime/dets/managed-public-otp-contracts`, `runtime/dets/original-public-otp-contracts`, `runtime/dets/managed-upstream-cases`, `runtime/dets/original-upstream-cases`, `runtime/library453/original-named-kernel-no-listener-full-outcomes`, `HOST/library419/ets-transfer-queue-monitor-order-and-recipient-death-fence`, `runtime/prim-tty412/real-redirected-resource-foreign-and-closed-owner`, `runtime/halt-live-port-drain-immediate-timeout-and-profile-ownership`, `runtime/seq-trace-concurrent-token-snapshots-tracer-switch-and-self-isolation`, `runtime/spawn-request-abandon-owner-identity-death-order-and-resource-failure`, `runtime/source-stage-transaction-ownership-and-purge-generation`, `host_guest_connected_exit_profiles_tokens_and_ownership`, `host_async_distribution_connected_staging_and_hard_bound`, `host_restored_local_fun_publication_and_lambda_dispatch`, `runtime/code-purge-old-users-pending-funs-and-current-retention`, `compiler/beam/gc-bif-keeps-call-result-root`, `compiler/beam/constructs-returned-siblings-from-live-call-roots`, `compiler/beam/promotes-only-post-call-projections-local-to-a-terminal-arm`, `compiler/beam/entry-values-after-frameless-branch-keep-owned-frame`, `oracle/compiled-module/if-tail-calls`, `oracle/compiled-module/scheduler_first_instruction`, `oracle/compiled-module/scheduler_migration_previous_binding`, `oracle/compiled-module/scheduler_invalid_and_duplicate`, `oracle/compiled-module/scheduler_offline_bound_resume`, `oracle/compiled-module/scheduler_offline_pending_kill`, `runtime/initializer615/terminal-mailbox-before-stop-cancel`, `runtime/genuine-vm686/offline-claimed-native-input-vm-close-before-entry`.

| Check | Actual result |
| --- | --- |
| Pinned release | OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 |
| Frozen payload | [Source, compiler and runtime](source-freeze.json), [compiler inputs](compiler-source-closure.json), [full launch](full-launch.json) |
| Focused regression | [552 passed](focused-verified.json), [exact coverage in two subruns](fast-splits/receipt.json) |
| Scoped release | [13 Equal and 295 Different](pilot.json) across 308 images |
| Uncached full release | [510 Equal and 856 Different](full-uncached/otp-corpus.json) in 522.81 seconds. Zero Unsupported, SourceRejected and CompilerError. |
| Raw changes | [All 510 previous exact images retained](full-uncached/compare-summary.json). 0 new exact images. [Section deltas](full-uncached/section-delta.json) record 5 gains and 0 losses. |
| Normalized instructions | [125 gains and zero losses](rank-counts.json), 76 frames closer and 0 farther |
| Native | [284 release observations and 323 synthetic comparisons](native-early-records/receipt.json), [pinned loader outcomes](loader-rollup.json), [seven supplementary native profiles](successor-native-receipt.json) |
| Remaining causes | [Corpus ranking](ranked-causes.md) |
| Windows build profiles | [Effective profiles agree](profile-equivalence.json) |
| Readiness | [Regenerated from the immutable managed test report](readiness-receipt.json) |

An outgoing argument follows independent frame kills. Proven selector arms share one table and checked record heads preserve their distinct failure paths. Qualified comprehension helper identities and singleton captures are retained. Closed list heads use separate source and head failures. Wide integer segments preserve their checked sign-fill schedule. The combined source also places mixed atom and tuple-arity partitions before branch-local frames, shares the nested state selector, reuses checked fallthrough projections, and carries exact local tuple result facts. Post-call constructor roots, the physical label count and catch reason registers use their original owners. Proven noncollecting head BIFs stay on a frameless leaf path.

The linked scope and runtime execution use the frozen compiler and release manifests. The full corpus recompiles every module. Native checks also exercise the real syntax annotation API, tagged record failures and the exact effects of list and case evaluation. The trailing-root witnesses expose selected private functions through temporary copies with only ExpT changed and assert identical Code. The loader rollup uses the unmodified compiler images.


The local [eight-module decoder timing comparison](performance-baseline-1971/receipt.json) uses a separate Part 1971 experiment.

The [local repository validation](local-general/receipt.json) records the outcome for this combined package.
