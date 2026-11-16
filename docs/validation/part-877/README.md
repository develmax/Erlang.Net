# Part877 Selective receive

The general emitter scans candidates in mailbox order, evaluates patterns and supported guards in clause order, removes the first accepted message and runs its body. Candidate scratch values stay in X registers during matching. The accepted bindings move to the function frame after message removal. A timed receive evaluates its timeout operand before scanning and executes the after body when the wait expires.

The permanent regression sends a nonmatching atom, a tuple rejected by the guard, a matching tuple and a stop message. It checks clause order, the timeout result and retention of the skipped atom. The original OTP beam and the C# beam both produce the same observation on OTP 29.1.1. [Reference and generated results](otp-receive-result.txt).

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run its DLL with the pinned installation root and report path. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`. Run whole-module-matches-otp, documentation-payload-matches-otp and rebuild-installed-otp-release-images. All three passed. [Focused report](focused.json).

The full runtime suite was outside this run. The readiness CLI requires a full test report and rejected the focused report, so the canonical readiness reports were not regenerated.

The full corpus covered 1,366 release images. It recorded 100 Equal, 74 Different, 164 SourceRejected and 1,028 Unsupported. The observer image moved from Unsupported to Different. The most frequent remaining specific expression failure is Apply, affecting 33 modules. [Corpus report](otp-corpus.json).
