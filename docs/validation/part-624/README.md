# Part624 LDAP image differences and frame slot reuse

The three-image comparison found 274 functions and 54,262 Dbgi bytes in the installed LDAP release. OTP compiling the C#-generated Erlang produced 137 functions with 7,842 Code bytes and 66 Dbgi bytes in deterministic mode. The earlier C# image had 134 functions and 11,161 Code bytes for that input. The release ASN.1 output and compiler build options differ from the C# path. The same-source comparison also exposes differences in instruction selection and generated helpers.

The general emitter now reads already-bound variables directly from their frame slots and skips unused tuple wildcard fields. The C# Code chunk shrank from 11,161 to 10,364 bytes and the whole image from 29,732 to 28,932 bytes. Eleven same-source runtime checks passed against OTP, comparing values and error reasons. Both focused tests passed. Release byte parity remains open.

The final corpus recorded 18 exact matches, nine differing images, 488 source rejections and 851 unsupported modules across 1,366 release files. [Per-image report](otp-corpus.json).
