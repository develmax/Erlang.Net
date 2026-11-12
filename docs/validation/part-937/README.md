# Part 937 explicit NIF declarations

Admit -nifs in BEAM source mapping, record the source attribute in Dbgi, and emit nif_start immediately after the entry label for declared functions. OTP keeps the source declaration in abstract code and excludes it from Attr. The legacy implicit load_nif policy and on_load emission are outside this change. [Pinned disassembly and abstract forms](reference.txt).

The deterministic marker fixture matches byte for byte. The local NIF call probe still differs in Code, ExpT and Attr. Three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 995 Different, 56 SourceRejected and 92 Unsupported. Seven modules now generate BEAM and prim_tty reached a later receive guard. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
