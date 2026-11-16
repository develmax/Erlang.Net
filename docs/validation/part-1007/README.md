# Part 1007 stdlib and kernel release profiles

Use the confirmed Windows release build profiles for stdlib and kernel, including the extra erl_epmd definitions. Keep compile information when a release profile explicitly permits incomplete abstract forms and the abstract writer reports an unsupported form. Read record field delimiters without treating function types as anonymous expression blocks.

Emit a frameless tail call for unconditional Erlang stubs that call erlang:nif_error/1 with an atom. Math, records and erl_debugger now match the release byte for byte.

The full Windows OTP 29.1.1 corpus has 308 Equal, 1055 Different and 3 SourceRejected. All prior matches remain Equal, including ELDAPv3. The CInf difference is gone for all 201 generated stdlib and kernel images. The Dbgi differences dropped from 193 to 110 and Docs differences dropped from 193 to 28 in those applications. [Corpus report](otp-corpus.json). [Chunk counts](chunk-counts.txt). [Release profile groups](profile-groups.txt). [Three focused checks passed](focused.json).
