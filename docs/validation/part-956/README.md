# Part 956 grouped operands in debug forms

Distinguish parentheses around an operand from parentheses around its entire expression. Record updates, field access, value calls and matches can continue after the closing parenthesis. Value call annotations use the first source annotation of the callee as in the pinned Erlang parser. The debug writer also reports the expected and actual symbol when token consumption fails.

All three focused checks passed. The fixture compares four decoded function forms against OTP 29.1.1. [Reference forms](reference.txt) and [focused checks](focused.json).

The full corpus has 223 Equal, 1107 Different, 29 Unsupported and 7 SourceRejected. Five eunit modules generate BEAM. Eunit_proc, eunit_test and eunit_tests match the release Dbgi bytes. ELDAPv3 remains Equal. Snmp_config reports a closing parenthesis expected before andalso. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
