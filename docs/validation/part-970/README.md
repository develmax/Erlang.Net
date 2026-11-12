# Part 970 record wildcard debug forms

The debug writer now preserves the explicit wildcard assignment in a record construction. OTP 29.1.1 describes it as a record_field whose name is a var form in lib/stdlib/src/erl_parse.yrl. The compiler accepts warn_unused_vars alongside its existing diagnostic options. No dependency was added.

The pinned erlc +debug_info +deterministic fixture verifies the decoded Dbgi function form. All three focused compiler checks passed. [Focused checks](focused.json).

The Windows OTP 29.1.1 corpus contains 223 Equal, 1137 Different and 6 SourceRejected. Both snmpa_symbolic_store and xmerl_ucs now emit complete images with differences. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
