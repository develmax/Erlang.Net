# Part 955 string sigil debug forms

Write the OTP UTF-8 binary AST for binary sigils and the string AST for list sigils. Preserve the distinct prefix and delimiter coordinates in source. When a macro substitutes the token, both coordinates follow the macro expansion location.

All three focused checks passed. The decoded forms match the pinned compiler for seven direct sigils and a function with two sigils expanded from macros. [Direct forms](beam_string_sigils.txt), [macro forms](beam_macro_sigil_probe.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1102 Different, 34 Unsupported and 7 SourceRejected. Beam_asm now generates BEAM. Its CInf bytes match, while Code and Dbgi still differ. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
