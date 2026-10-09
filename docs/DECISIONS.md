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
12. The local exact-version Erlang runtime is absent. Remote differential execution builds the pinned source in isolation; run 37938184184 built successfully and matched 16 cases before an invalid Unicode source literal. Part 009 corrects the earlier transport diagnosis: explicit UTF-8 decoding in a follow-up run still failed because erl_scan excludes U+FFFF. The runner checks the exact release, compares tagged value/error outcomes and does not normalize meaningful term differences. Part 008 passes source as Base64 UTF-8 through an ASCII command, then explicitly scans/evaluates codepoints. Partial reports distinguish infrastructure aborts from semantic failures. Complete stacktrace/side-effect/signal comparison remains a separate future track.
13. Map pattern keys evaluate against the scope preceding the whole pattern, separate from newly bound value patterns. Closures preserve captured key scope while their value-pattern variables shadow outer variables. Optional process context reaches nested patterns for guard BIFs such as self.
14. The user authorizes publishing completed commits to the configured Git remote. Public NuGet publication remains deferred. Keep detailed English entries and immutable per-part validation reports; do not overwrite historical evidence when updating current reports.

## Part 018 — Semantic constants and readable code

Group reusable constants by semantic ownership, never by literal equality alone. Error reasons and exception classes are separate term-domain files; compiler bit segment names, byte orders, syntax defaults, floating widths, storage layout and Unicode limits have distinct groups. Lexer token kinds are not bit segment types; BIF names are not exception classes; integer default width is not bits per byte; UTF32 length is not maximum UTF encoding length. Keep source-language fixture text and one-off diagnostic messages near their use unless an actual shared contract exists. Use .editorconfig and logical blank-line separation for future changes; the hybrid source remains subject to its own preprocessing pipeline.

## Part 019 — Compiler diagnostic catalog clarification

The user's screenshot follow-up includes one-off compiler diagnostic strings, not just repeated values. This supersedes the earlier decision to keep such compiler messages inline: all CompileException codes and texts now belong to CompilerDiagnostics.cs, grouped by lexer/parser/semantics/bit-pattern/hybrid ownership. Ten named builders retain original interpolation for dynamic diagnostics. Source-language fixtures stay local and independent. LexerTokenKinds.Integer/Float remain separate from BitSegmentTypes.Integer/Float. BitSpecifierCategories.Unit (conflict key) and BitUnitSpecifier.Name (source spelling) remain separate despite both being unit. Limits of a source unit are distinct from default units with equal values.

## Part 020 — One namespace-level type per file

The user's file-layout requirement supersedes composite type files, including the part-019 shared diagnostic catalog. Each namespace-level class/record/struct/interface/enum/delegate lives in TypeName.cs. Nested types remain in their owning declarations. Top-level executable statements stay in Program.cs and helpers move into their own files. Namespaces, access modifiers, generic parameters, attributes, type/member tokens and source values remain unchanged. Original file using directives are copied to extracted types to preserve resolution; unused imports are harmless and were not independently simplified. CompilerDiagnosticCodes, LexerDiagnostics, ParserDiagnostics, SemanticDiagnostics, BitPatternDiagnostics and HybridDiagnostics now each have a separate file, as do bit-constant groups. The package-consumer fixture copies every root HelloHybrid C# file so moved fixture types are included.
