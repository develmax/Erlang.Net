# Part 1026 remaining release compile information

Recover the remaining release build options for Public Key, wx, Megaco examples, SNMP examples, Common Test extensions, Crypto, LDAP and ODBC. Preserve application version attributes, macro definitions and the distinct Windows build path for gl.

| Chunk | Differences before | Differences after |
| --- | ---: | ---: |
| CInf | 31 | 0 |
| Dbgi | 31 | 19 |
| Docs | 28 | 4 |

These counts cover the 31 entries with compile information differences in the preceding run. All generated images now match CInf. The full Windows OTP 29.1.1 corpus has 332 Equal, 1031 Different and three SourceRejected. All previous exact images remain Equal, including ELDAPv3.

- [Full corpus](otp-corpus.json)
- [Three focused checks](focused.json)
