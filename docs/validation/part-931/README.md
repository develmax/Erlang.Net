# Part 931 public_key ASN.1 release profile

Reuse one release profile for 31 public_key ASN.1 modules. The compiler version and options, source path and relative file names follow the pinned release. The compiler reconstructs CInf, Dbgi and Docs from source and these parameters. [Pinned compile info](compile-info.txt) and [file annotations](file-annotations.txt).

The 31-module cohort has 3 Equal and 28 Different. All 31 now match CInf, Dbgi and Docs. Line differences fell from 30 to 26. [Cohort report](asn1-cohort.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 936 Different, 56 SourceRejected and 151 Unsupported. PKIX-CommonTypes-2009 and RFC5639 moved from Different to Equal. ELDAPv3 remains Equal. [Full report](otp-corpus.json) and [status changes](changed-status.txt).

Both builds and all three focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. Reuse the include directories from [Part 930](../part-930/headers.md) for reproduction.
