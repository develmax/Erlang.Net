# Part 929 release include inputs

The corpus runner can take an external directory of generated public_key headers. It also resolves the shipped kernel inet.hrl for common_test. The original release installation is used as the source and byte-comparison reference. [Header regeneration](headers.md).

All 1,366 release files were compared. 38 moved from SourceRejected to Different. 221 are Equal, 931 Different, 67 SourceRejected and 147 Unsupported. There are 12 remaining first failures on include resolution. [Full report](otp-corpus.json) and [status changes](changed-status.txt).

The corpus tool built with zero warnings and errors. A missing optional header directory exits with code 2. The full runtime suite and canonical readiness regeneration were outside this run.
