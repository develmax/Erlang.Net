# Part 989 wx helper order and text setter moves

Emit the extracted wx Unicode helpers among lifted anonymous functions in reverse lexical order. Resolve their local entry labels with the existing deferred label mechanism. Move the converted text into its call register before restoring the index in three-argument tail setters, following the release disassembly. No dependency was added.

The intermediate wxControlWithItems comparison matched the helper order and exposed the remaining setString move order. The final deterministic fixture covers both changes and matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json), [helper order](helper-order.txt), [intermediate difference](control-diff.txt) and [move survey](tail-moves.txt).

The full Windows OTP 29.1.1 corpus has 267 Equal, 1096 Different and 3 SourceRejected. WxControlWithItems is the new exact match and ELDAPv3 remains Equal. [Corpus](otp-corpus.json). The generated wx survey still identifies five missing comprehension helpers. [Remaining helpers](remaining-list-helpers.txt).
