# Part625 OTP LDAP header and export parity

The C# ASN.1 writer emits the exact 2,917-byte ELDAPv3.hrl from the pinned schema. Its SHA-256 is F7CCA5A2F3FCBB330B317A712D1602882DEDC392FBC7FB8496537897B444F68A. The test compares against the original header stored with the upstream module.

The generated Erlang now includes the Dialyzer suppression export. The C# Docs writer quotes Erlang function names when needed. The installed OTP VM loaded the resulting C# image. Its 111 exports matched the release, the suppression function returned ok, and the MessageID BER round trip passed. All three focused tests passed.

The installed ELDAPv3 compile info records compiler 10.0.4, the legacy Erlang types option, debug info and release source and include paths. Its debug forms retain the generated header and ASN.1 generator metadata. The generated Erlang bodies, optimizer output and release metadata still differ. The BEAM image remains byte-different.

The final 1,366-image corpus run recorded 18 exact matches, nine differing images, 488 source rejections and 851 unsupported modules. [Per-image report](otp-corpus.json).
