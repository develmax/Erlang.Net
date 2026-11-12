# Part 1010 release profiles and singleton-list tail calls

Apply the confirmed megaco, SSL, SSH and Common Test application profiles. Use the Windows compiler version, source paths and definitions for the two flex modules. Correct the recorded include path for inets_internal.hrl in http_lib and megaco.hrl in the UDP group. Emit an in-place singleton list before a positional remote tail call.

All 232 generated application images in the four profile groups now match CInf. The 187 previous CInf differences are gone. Their Docs differences fell from 187 to 19 and Dbgi differences fell from 187 to 122. Four more inets images match Dbgi. [Chunk counts](chunk-counts.txt). [Release profile groups](profile-groups.txt).

The full Windows OTP 29.1.1 corpus has 310 Equal, 1053 Different and 3 SourceRejected. Megaco_udp_sup is the new exact image. All previous matches remain Equal, including ELDAPv3. [Corpus report](otp-corpus.json). [Three focused checks passed](focused.json).
