# Compatibility checkpoint

Target: OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation is incomplete. [Matrix](compatibility-matrix.md), [semantic boundaries](semantic-differences.md), [source inventory](otp-inventory.md) and [direct MFA tests](supported-mfas.json) define the current evidence.

Local regression results cover a language/runtime subset, not every OTP export. The independent production execution path is C#/.NET; BEAM is used only by the optional development differential runner. No reference runtime is installed on this host. OTP reference suites and real-node interoperability have not run.

Each implemented function remains Partially compatible until full applicable inputs/errors/side effects and exact-version oracle evidence are available. An implemented process primitive must not be confused with complete process signal/scheduler compatibility. A generated AST emitter must not be described as a complete Erlang compiler.

Machine-readable source defaults are Not started. `supported-mfas.json` is the overlay marking registered MFAs Partially compatible and naming their tests. Source duplicates/conditional exports remain separate from effective release API coverage.

Part 004 adds source map construction, associative/exact updates and subset patterns with guard-expression keys, duplicate/repeated bindings and badmap/badkey checks. These remain Partially compatible; permanent local and generated-source tests establish only their tested boundaries. Map comprehensions and the complete map BIF/guard set are pending.
