# Outgoing argument scratch-copy elimination

The earlier compiler produces X0 to X2, the independent argument load into X1, then X2 to X0 before erlang:exit/2 in snmp_app_sup:stop/0. OTP 29.1.1 retains X0 and only writes X1. The pinned beam_ssa_codegen:gen_moves and copy helpers omit identity copies and order moves by their dependencies. beam_block sorts X/Y spill moves without introducing an X/X roundtrip. beam_validator validates the live arity prefix of X registers at these calls.

The C# pass works on contiguous moves to X registers before an ordinary or tail local or external call. It removes a final restore into X0 and its matching save only when the temporary is outside the call arity and no intervening move reads or writes either register. It preserves instruction order and line operands. The existing call move pass already supplies the instruction boundaries.

The stand-alone five-function fixture is byte-identical to pinned OTP after the change. The native observations prove shutdown and its DOWN message on a registered process, missing-name handling and the continued availability of repeated outgoing PID arguments. The permanent regression checks the ordinary and tail call sequences plus a live third argument. The focused harness reproduces the new assertion failure on the preceding compiler and passes with the candidate.

The eight-module cohort remains 4 Equal and 4 Different, with no SourceRejected or Unsupported. It contains four release images with confirmed removable pairs and four already-equal controls. The exact release snmp_app_sup:stop/0 sequence now matches OTP. All four changed candidate BEAMs, their predecessor images and the four pinned counterparts pass native code:prepare_loading. The cohort comparison took 13.460 seconds before and 12.569 seconds after with one worker. These are sample wall times from the shared host.

The complete saved part 1832 output scan found 51 removable pairs spanning 40 modules, with no occurrences in the pinned originals and zero decoder failures. Projecting only these deletions onto the saved files produces zero new whole-file or Code matches. The scan did not recompile any module. The eight-module cohort was recompiled.

See before.json, after.json, scan-final.json, the *.terms and *.log files, and outgoing-x0-roundtrip.patch. The full suite and a new full release corpus were outside this package's isolated validation scope.
