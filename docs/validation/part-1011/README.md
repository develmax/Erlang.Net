# Part 1011 Diameter release profiles

Select the Windows OTP 29.1.1 Diameter release profile for sources in base, transport, gen, compiler and info. The profile uses the compiler flags and inserted application version from the pinned Diameter Makefile and the build paths recorded in the release images.

| Diameter chunk | Differing images before | Differing images after |
| --- | --- | --- |
| CInf | 41 | 0 |
| Dbgi | 41 | 9 |
| Docs | 41 | 8 |
| Attr | 41 | 41 |

All 47 Diameter modules emit BEAM and six match their complete release images. The full corpus has 310 Equal, 1053 Different and three SourceRejected. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Focused compiler checks](focused.json)
