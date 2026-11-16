# Part 1009 inets and SNMP release profiles

Select release profiles by source subdirectory and preserve existing module-specific profiles. Apply the recorded build definitions and app_vsn insertion for the main applications. When an opted-in profile encounters an unsupported abstract form, retain the release attribute and compile information and emit minimal debug information.

All 63 inets and 90 SNMP application images now match CInf. Docs differences fell from 57 to zero in inets and from 69 to 2 in SNMP. Dbgi differences fell from 57 to 44 in inets and from 72 to 67 in SNMP. Ten images still have minimal debug information. These counts cover the main application images. [Chunk counts](chunk-counts.txt). [Targeted comparisons](targeted-chunks.txt).

The full Windows OTP 29.1.1 corpus has 309 Equal, 1054 Different and 3 SourceRejected. Every previous match remains Equal, including ELDAPv3. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).
