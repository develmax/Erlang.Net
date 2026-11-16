# Part 973 type lint option and wx cohort

Admit the no_auto_import_types source option. OTP 29.1.1 erl_lint uses it to suppress the built-in type redefinition warning. It does not change instruction emission. Retain the directive for release metadata through the existing compile option path. The compiler has no corresponding built-in type redefinition warning. No dependency was added.

The focused compiler checks passed. The full Windows OTP 29.1.1 corpus has 223 Equal, 1140 Different and 3 SourceRejected. The erlang module now emits BEAM. All 223 exact matches, including ELDAPv3, are preserved. [Focused checks](focused.json) and [corpus](otp-corpus.json).

The largest shared profile candidate is the wx generated group. The compile_info inspection found 235 modules with the shared bootstrap profile, including 139 of the 140 outstanding generated modules. [Profile inventory](wx-profile.md).
