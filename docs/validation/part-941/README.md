# Part 941 external documentation

Read relative file documentation and preserve its file and location annotations and the return transition. Use pinned Markdown inputs for the six stdlib modules. [Inputs](inputs.md), [OTP probe annotations](reference.txt) and [release annotations](release-annotations.txt).

The deterministic module and function documentation probe matches byte for byte. The three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1013 Different, 44 SourceRejected and 86 Unsupported. Four modules now generate BEAM and two reach later generation blockers. The erlang module reports an additional missing Markdown input. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
