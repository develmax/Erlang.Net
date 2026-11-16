# Part 972 qualified specs and ERTS documentation

The source parser accepts a module qualifier on specs for the current module. The debug form retains the {Module, Function, Arity} key from the pinned OTP erl_parse.yrl. Qualified callbacks and specs targeting a different declared module are rejected following erl_lint.erl. No package was added.

The qualified spec fixture compares decoded Dbgi specs and Docs payloads. Its final OTP 29.1.1 reference uses erlc +debug_info and the explicit source path from the build profile. The initial deterministic reference omitted source_path and failed the Docs comparison. The corrected reference passed all three focused checks. [Initial run](initial-focused.json) and [final run](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1139 Different and 4 SourceRejected. Erts_internal now emits BEAM. ErLang's source reaches no_auto_import_types after resolving both external Markdown files. ELDAPv3 remains Equal. [Input setup](inputs.md) and [corpus](otp-corpus.json).
