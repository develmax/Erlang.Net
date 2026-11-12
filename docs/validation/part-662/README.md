# Part662 LDAP constructed ASN.1 source layout

The C# ASN.1 writer follows the OTP constructed BER layout for nested encoder headings, choice branches, optional and default fields, constrained integers, octet strings and collection decoder headers. The changes apply by ASN.1 shape. The generator preserves the abstract forms and debug options after removing annotations.

All 18 absolute line instructions for enc_SearchRequest/2 match OTP 29.1.1, starting at line 1635. Its 155 normalized instructions and arithmetic operands still match. The top-level coordinate mismatch count drops from 251 to 25. The first remaining mismatch is dialyzer-suppressions/1 at 3392 against 3399. [Line comparison](search-arithmetic-oracle.txt), [top-level coordinates](coordinates-oracle.txt).

The generated module is 108,272 bytes against 99,008. AtU8, ImpT, StrT, CInf and Meta remain byte-exact. Code and eight other sections still differ. Dbgi is 54,238 bytes against 54,262. [Chunk comparison](chunks-oracle.txt).

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the C# BER writer and follows the source layout in asn1ct_constructed_ber_bin_v2.erl. No dependency or license changes.

Built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part662-coordinate-tool` and ran `dotnet artifacts/beam-backend/part662-coordinate-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part662-coordinate.json`. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full runtime harness and canonical readiness regeneration remain unrun.

All three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type errors. The full corpus checked 1,366 modules with 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 remains Different. [Focused report](focused.json), [corpus report](otp-corpus.json).
