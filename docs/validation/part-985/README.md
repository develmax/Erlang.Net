# Part 985 static wx text and option commands

Reuse the command emitter for two arguments without an object record. Save the options in one Y slot and reuse it for converted chardata. Convert an option whose input starts with the option value and ends in literal list elements by emitting a cons with a constant tail, as in wxArtProvider's client option. The initial getBitmap/2 output still used the generic frame and the final allocation matches OTP 29.1.1. No dependency was added.

The deterministic fixture with static text conversion, option mapping and a literal text suffix matches OTP byte for byte. All three focused checks passed. [Focused checks](focused.json), [initial getBitmap allocation](initial-get-bitmap.txt) and [final allocation](get-bitmap.txt).

In 235 generated wx images, 14 additional first frame sizes match the release. The full Windows OTP 29.1.1 corpus has 262 Equal, 1101 Different and 3 SourceRejected. WxArtProvider and wxNotificationMessage are new exact matches. ELDAPv3 remains Equal. [Frame counts](frame-counts.txt), [opcode comparison](wx-opcodes.txt) and [corpus](otp-corpus.json).
