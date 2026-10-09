# Decisions

1. Pin stable OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The existing reference HEAD is newer and is not the baseline.
2. Pin .NET SDK 10.0.400 and C# 14, net10.0. Avoid external restore dependencies in the initial runtime/test bootstrap.
3. Use distinct term classes, explicit exact/numeric comparison, immutable cons cells/maps/buffers and Erlang exceptions. No object/null-based term contract.
4. Use ProcessContext plus async continuations and reduction checkpoints. Tasks are scheduler infrastructure; local lifecycle, signals and selective receive are separately implemented.
5. Use dedicated Erlang lexer/parser/analyzer; borrow Roslyn only for C# lexical context. No regex language rewriting and no Roslyn fork.
6. Emit checked AST construction in generated C# as the first compilation representation. Keep direct lowering and optimized IR as explicit future compiler tasks.
7. Adjust the mandatory sample to an async process callback. Suspending receive must not block a C# public synchronous method or silently change its signature.
8. Supply #line plus original line-count preservation now; full expression columns and debugging require a later source-map representation.
9. Maintain source inventory separately from implemented MFA coverage. Classifications are provisional until component-level reuse/port audits are done.
10. Keep package/tool prototypes local, with a real PackageReference consumer. Public license selection and publication are deferred.
11. PowerShell validation and Bash commands in the development-only oracle CI workflow are non-C# test-infrastructure exceptions. Runtime, compiler, inventory, differential runner and benchmark code are C#.
12. The local exact-version Erlang runtime is absent. Remote differential execution builds the pinned source in isolation; the first build failure was diagnosed and a repaired run is in progress. The runner checks the exact release, compares tagged value/error outcomes and does not normalize meaningful term differences. Complete stacktrace/side-effect/signal comparison remains a separate future track.
13. Map pattern keys evaluate against the scope preceding the whole pattern, separate from newly bound value patterns. Closures preserve captured key scope while their value-pattern variables shadow outer variables. Optional process context reaches nested patterns for guard BIFs such as self.
14. The user authorizes publishing completed commits to the configured Git remote. Public NuGet publication remains deferred. Keep detailed English entries and immutable per-part validation reports; do not overwrite historical evidence when updating current reports.
