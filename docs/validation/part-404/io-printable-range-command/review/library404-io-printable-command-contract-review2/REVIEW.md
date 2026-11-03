# Successor2: exact pinned authority for printable_range command verdict

The original sealed review1 bound ../otp working-tree files. That sibling checkout is newer than the admitted pin; those live bindings were not sufficient pinned source authority. Review1 remains immutable history, and must not be cited as the pinned exhaustive proof. This successor supplies the actual pinned bytes and confirms the same narrow command verdict against them.

Authority is commit ad05823719d77c8faee87348ea39513d4e2f99c5, verified as a commit object. For each of4 relevant files capture-pinned.py executes git show PIN:path to bytes, resolves the exact blob object, verifies git hash-object --stdin equals that blob, stores the bytes under authority/, and records SHA256/byte length. No checkout, reset, build, compiler, native/managed guest or shared mutation was performed. Live-file SHA values are explicitly comparison-only; all4 live files differ bytewise from pin.

Exact numbered sections in pinned-numbered-excerpts.md:

- erl_unicode.c2943–2950: complete io_printable_range_0, exactly Unicode/Latin1 atom returns, no argument/ordinary error/allocation/permission/state mutation branch.
- erl_sys_common_misc.c62 and80–87: static defaultLatin1 and complete setter/getter. Setter is a startup state assignment; getter only returns it.
- erl_init.c1371–1383: startup -pc valid unicode/latin1 mappings, Latin1 initial temporary, invalid-string usage rejection, sole setter invocation.
- io.erl218–245: pinned exposed startup option/default/two valid values, exact spec and nif_error(undefined) native stub. The live documentation mentions newer additional consumers; those added descriptions are not used as pinned contract evidence.

pinned-references.json is produced by git grep against the explicit commit, never current worktree: setter references in ERTS are only startup call, definition and sys.h prototype; getter references only native body, definition and prototype. Pinned bif.tab619 records bif io:printable_range/0; pinned io export86/spec243 expose this exact signature. This closes the asserted finite-state source domain. Body-crosscheck separately confirms getter/setter/startup bodies match their live counterparts despite other file/line-ending changes; equality is reassurance, not a replacement authority.

Verdict after checking PIN: YES, the explicit original-source command io:printable_range/0 is complete within this pinned runtime/startup domain. Default/explicitLatin1/Unicode mapping and readonly .NET owner state cover every guest-observable getter return branch. Wrong host options/foreign/dead/canceled ProcessContext are integration boundary controls, not additional native argument-error cases for a0arity infallible getter. The source-selected original io image/capture ownership requirement remains explicit; this is not default autoload/whole io/whole io_lib/module_info/ERTS CLI compatibility.

All retained runtime/original evidence is unchanged: admitted29.1.1 collector remains the baseline, and sealed review1 passive extraction18/18 complete getter rows remains valid. Current .NET source identities are separately rebound below; original source authority comes only from this successor's pinned bytes. No new test execution is retroactively claimed. The72 io_lib:format error outcomes remain required failures; the540 comparison suite is not promoted. Existing default708/full118 ledger is not edited or expanded here. Parent owns final scoped documentation under existing rules.
