# Part 965 closures in targeted inline templates

Rewrite each closure clause with a fresh parameter scope and preserve capture substitutions. The new template path accepts variable, literal, alias, tuple and list parameter patterns. The fixture matches OTP behavior for capture and parameter shadowing. Its emitted bytes still differ. [Previous probe](probe-before.json), [current probe](probe.json), [reference values](reference.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1129 Different, 7 Unsupported and 7 SourceRejected. Erpc, sofs and xmerl_scan generate BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
