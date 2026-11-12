# Part813 HTML status dispatch and release metadata

The compiler omits the record tag test in the private status function only when local call sites establish the record shape. The status handler's instructions and line numbers match OTP after accounting for shifted label numbers. A pinned-runtime probe confirms the cdv status call, the obs fallback and the invalid record error. [Instruction comparison](status.txt), [behavior script](behavior.eval) and [results](behavior.txt).

The release profile reconstructs compile options and include paths. The debug writer retains source record updates and string prefix operators. All 143 decoded forms and options match. CInf, compressed Dbgi and Docs match byte for byte on the pinned Windows release and have a permanent regression check. The full image still differs in nine chunks. [Debug comparison](debug-summary.txt), [comparison script](compare.eval) and [chunk comparison](comparison.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part813-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part813-tests`.

The 1,366-file corpus reported 69 Equal, 2 Different, 195 SourceRejected and 1,100 Unsupported. Statuses are unchanged. All four focused tests passed, including the new metadata regression. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next step is to align the two window paths in expand/3. [Disassembly](expand-disassembly.txt).
