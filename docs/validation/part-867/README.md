# Part867 Record construction and wx debug metadata

The emitter expands traditional record construction into tagged tuples using OTP declaration order and the existing defaults and wildcard expansion. Simple returned records use the register constructor. Computed fields use the general expression emitter.

The deterministic constructor fixture checks reordered named fields and a default field. Regenerate it with pinned `erlc.exe +deterministic beam_constructor_probe.erl` in the fixture directory. The runtime probe checks a computed field and two wildcard-initialized fields. OTP returned `{sample,probe,"probe",missing,missing}` for the same source and inputs.

The debug writer distinguishes a parenthesized dynamic module operand from parentheses around the complete expression. The shared wx profile restores the verified compile and debug options for 11 additional generated wx modules. Their CInf, Dbgi and Docs match. Code and related chunks still differ.

The record-only corpus run reported 99 Equal, 15 Different, 164 SourceRejected and 1,088 Unsupported. The 53 former record-construction failures moved to three Different and 50 subsequent unsupported checks.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the built tool with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The final corpus has the same status counts. The three new Different images are ssh_tcpip_forward_srv, ssl_dh_groups, and xmerl_xpath_lib. All three focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
