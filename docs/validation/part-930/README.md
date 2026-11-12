# Part 930 source-tree include inputs

Six headers from the pinned OTP source tree resolve all 12 remaining first failures on include lookup. The runner uses the upstream relative paths and keeps the installed source and expected BEAM inputs separate from the include directory. [Header setup](headers.md).

The full Windows OTP 29.1.1 corpus reported 221 Equal, 938 Different, 56 SourceRejected and 151 Unsupported. Seven rejected sources now generate BEAM and four reach unsupported emission. The remaining source reaches the functional r26 compile option. ELDAPv3 is Equal. [Corpus report](otp-corpus.json) and [changed statuses](changed-status.txt).

The corpus tool built with zero warnings and errors. A missing source header directory exits with code 2. The full runtime suite and canonical readiness regeneration were outside this run.
