# Part 957 nested guard parentheses

Keep parser token identity for binary and match operators during release parsing. The debug writer follows the leading operand chain and checks whether the specific operator lies after the closing parenthesis. This distinguishes the inner and outer andalso tokens in a macro-expanded guard.

All three focused checks passed. The fixture compares the decoded function form and coordinates with pinned OTP 29.1.1. [Reference form](reference.txt) and [focused checks](focused.json).

The Windows corpus has 223 Equal, 1108 Different, 28 Unsupported and 7 SourceRejected. Snmp_config now generates BEAM and its CInf matches. Code and Dbgi differ. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
