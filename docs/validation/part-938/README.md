# Part 938 targeted inline patterns

Rewrite aliases, map keys and binary segment sizes in inline templates. Admit supported patterns in receive, try and catch forms. Preserve the existing local binding and argument evaluation rules. [Pinned disassembly](reference.txt) and [OTP values](values.txt).

The three focused checks passed. The deterministic probe still differs in Code, ExpT, Attr and Type. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1005 Different, 56 SourceRejected and 82 Unsupported. Ten modules now generate BEAM. Three modules remain at targeted inlining, down from 21. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
