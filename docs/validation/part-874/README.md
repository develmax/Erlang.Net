# Part874 Numeric equality and tagged list results

The emitter lowers numeric equality and inequality to BEAM tests in guards and BIFs in value expressions. It uses exact equality for atom literal operands and preserves OTP's branch layout in direct predicates. The runtime fixture checks integer and float comparison in a guard and in boolean expressions.

The deterministic fixture uses pinned `erlc.exe +deterministic beam_numeric_compare_probe.erl`. The separate tagged-list case emitter follows the pinned release disassembly for a two-branch case that preserves an argument across an imported call.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The final corpus reported 100 Equal, 69 Different, 164 SourceRejected and 1,033 Unsupported. xmerl_sgml matches the complete release file. httpd_esi, xmerl_b64Bin_scan and xmerl_xpath_scan newly emit different images. All three focused tests passed, including the permanent xmerl_sgml release check. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
