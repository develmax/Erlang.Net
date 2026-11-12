# Part 936 documentation equiv expressions

Format map literals, string arguments, signed integers and simple anonymous functions in documentation equiv calls. Follow the OTP pretty printer for qualified auto-imported BIFs and the multiline function forms used by the release corpus. [Pinned metadata](equiv.txt).

The deterministic fixture matches OTP byte for byte and the permanent test compares the Docs payload. Three focused checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_docs_equiv_forms.erl), [probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 988 Different, 56 SourceRejected and 99 Unsupported. Ten modules now generate BEAM and os_sup reached its init/1 body. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
