# Part894 Release metadata and callback documentation

The Windows OTP 29.1.1 corpus supplied 37 candidates whose Code chunks already matched. Shared profiles reconstruct the release build metadata for those modules. Docs follows local type references from callbacks, quotes callback names when required and uses the recorded include path for exported types declared in headers.

The focused 37-module corpus matched 31 complete images, including ten wx event modules and SNMP, SSH, SSL, Megaco, TFTP, Common Test and Inets modules. Six still differed in Dbgi. The remaining differences are include transition paths or coordinates, map type field positions, a character literal tag and a remote type annotation.

Rebuild the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned Windows installation and report paths. The focused runner selects the whole-module, Docs and installed release image cases. Full runtime and canonical readiness checks remain pending.

The full 1,366-file corpus reported 131 Equal, 715 Different, 164 SourceRejected and 356 Unsupported. All 31 status changes were Different to Equal. [Corpus report](otp-corpus.json).
All three focused compiler tests passed. [Focused report](focused.json).
