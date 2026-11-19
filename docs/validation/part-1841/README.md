# Part 1841 — Keep one-use call results rooted in X0

The C# BEAM pass retains the X0 result of a local or external call
when its Y copy has a single use in the next collecting BIF. It
maintains the BIF's live X prefix and keeps the old slot initialized
until ordinary frame compaction removes it. The proof excludes trims,
handlers and non-exiting BIF failure edges. The permanent control
keeps an input copy needed after the operation.

The package also expands closed atom dispatch for aliases that return
the selected input and finite atom equality guards. The grouped
selector uses the existing failure edge and OTP order. Both changes
follow the pinned SSA and code emission sources.

The cohort covers 122 modules, including all 84 modules with a
proved removable save in the saved-output audit. It completed in
251.36 seconds on one worker and reports 6 Equal, 116 Different,
0 Unsupported and 0 SourceRejected. The one exact gain from Part
1839 is ssl_dh_groups, already established in the Part 1840 scope.
No earlier whole-file match was lost. Pinned ERTS prepared all 90 changed
files. Seventeen normalized functions gained equality with zero losses.
The separate native execution traces in the prototypes cover positive
and negative cases. Twenty-two named integrated tests passed.

The profile snapshot invokes the compile option and physical include resolvers from the frozen corpus runner without compiling a module. It records 1,366 distinct profiles, including preprocessor definitions, recorded options, recorded include paths and physical include search order. Source, BEAM, supplemental header and ASN.1 generator hashes remain in the input manifests.

[Corpus](cohort.json) · [Timing](cohort.json.timings.json) ·
[Normalized instructions](normalized-delta.terms) · [ERTS preparation](loader.terms) ·
[Focused tests](focused.json) · [Source identity](source-freeze.json) ·
[Resolved build profiles](windows-build-profiles.json.gz) ·
[Collector](collect-build-profiles.ps1) · [Inputs](../part-1823/windows-release-inputs.json) ·
[Input verification](../part-1832/release-input-verification.json).
