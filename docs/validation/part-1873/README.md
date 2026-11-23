# Windows source provenance and projected call argument lifetimes

The source-derived Observer, Reltool and Et profiles preserve each
ordered build include root and the original names of the libraries
under the in-tree lib directory. The Inets profile distinguishes
relative include, explicit include root, original application fallback
and bootstrap fallback. The whole-file comparison reads the installed
reference images only to compute equality.

The exact integrated compiler passes all 31 selected cases from 9,827.
The 171-file uncached comparison finishes in 180.27 seconds with
37 Equal, 134 Different, 0 Unsupported and 0 SourceRejected. No
complete file transitions. All 103 changed images prepare in pinned
ERTS. Fifty-eight Dbgi sections gain raw equality with no losses.
The integrated tool captures all 319 effective include maps.

The two isolated identical-compiler profile comparisons confirm
34 Dbgi gains across 59 Observer, Reltool and Et modules and 24 more
across 63 Inets modules. Other sections remain byte-identical within
each profile experiment. Native ERTS fixtures confirm source versus
bootstrap precedence and local header shadowing. GUI and Inets use
disjoint module paths.

Seventeen existing normalized functions gain equality against Part
1870 and none lose it in scope. Thirty-seven frames move closer.
Eleven of the twelve earlier normalized losses restore, including
the projected ASN.1 call and fprof guarded projection. The independent
clobber-sinking unit case passes, but the real diameter_make body
still differs. Its exact release ingress requires additional analysis.

The first Inets integration check exposed a project-file line-ending
mismatch and a trailing blank line. The corrected patch application
preserves intended changes, and the formatter check passes. No
complete managed acceptance or fresh full release result is claimed.

[Exact tests](focused.json) · [Release result](pilot.json)
· [Raw section gains](compare-1870/pilot-section-delta.json)
· [Normalized bodies](compare-1870/pilot-summary.terms)
· [Effective include maps](effective-include-maps.json)
· [Profile evidence](gui/) and [Inets evidence](inets/).
