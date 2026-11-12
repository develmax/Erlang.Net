# Part 1025 release profiles for nine applications

Apply the recorded build options for edoc, runtime_tools, reltool, syntax_tools, os_mon, tftp, et, ftp and parsetools. Preserve the FTP version define and Reltool application version insertion. Suppress the Docs chunk for the edoc release profile, which uses no_docs.

| Chunk | Differences before | Differences after |
| --- | ---: | ---: |
| CInf | 76 | 0 |
| Dbgi | 76 | 43 |
| Docs | 56 | 5 |

The table covers 84 release entries, including one rejected source. The full Windows OTP 29.1.1 corpus has 332 Equal, 1031 Different and three SourceRejected. All previous exact images remain Equal, including ELDAPv3.

- [Full corpus](otp-corpus.json)
- [Three focused checks](focused.json)
