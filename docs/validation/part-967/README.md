# Part 967 documentation equivalence expressions

Format named fun references, arithmetic expressions and character constants in equiv metadata. Keep the character token distinction through parsing and preserve precedence in nested arithmetic. The expanded fixture matches the uncompressed OTP Docs payload. All three focused checks passed. [Reference metadata](reference.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1132 Different, 4 Unsupported and 7 SourceRejected. Global and string emit BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
