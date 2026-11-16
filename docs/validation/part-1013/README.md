# Part 1013 wx examples and additional release profiles

Recover the build options from the pinned release for the wx examples, ASN.1 compiler, preloaded ERTS modules, SASL and Tools. Record the absolute megaco.hrl path for the TCP group, matching the OTP abstract forms.

| Group | Images | CInf differences before and after | Dbgi differences before and after |
| --- | ---: | ---: | ---: |
| wx examples | 33 | 33 → 0 | 33 → 33 |
| ASN.1 | 22 | 20 → 0 | 20 → 13 |
| ERTS | 22 | 22 → 1 | 22 → 8 |
| SASL | 18 | 17 → 1 | 17 → 5 |
| Tools | 16 | 16 → 0 | 16 → 9 |

The remaining CInf differences in these groups are prim_eval and the SASL example target_system. The full corpus has 315 Equal, 1048 Different and three SourceRejected. Persistent_term and megaco_tcp_connection_sup are the new exact images. ELDAPv3 remains Equal.

- [Full corpus](otp-corpus.json)
- [Three focused checks](focused.json)
