# Windows SNMP include provenance

The compiler preserves the exact ordered logical include path selected
under a mapped build profile. The profile expresses the shipped SNMP
makefile search paths and the in-tree and bootstrap application roots.
The physical resolver follows the available release headers. The
recorded path retains literal path components and nested include order.
Nonmapped profile controls use their existing physical resolution.

The isolated compiler and corpus tool build cleanly. All 25 selected
managed cases pass from 9,821 registered cases. The fresh 105-path
selection at two workers finishes in 62.58 seconds with 30 Equal, 75
Different, 0 Unsupported and 0 SourceRejected. No file status changes.
All 64 changed files prepare. Fifty-eight raw Dbgi sections gain
equality. No previously matched raw section loses equality. The exact
effective maps and installed source hashes are recorded for all 93
SNMP paths. The first reflection probe wraps a CLR string in a shell
object. Its diagnostic failure is retained and the corrected probe
uses exact CLR argument values.

An independent paired run on the frozen Part 1866 compiler confirms
the same 58 complete Dbgi gains and no file status changes on all 93
SNMP images. Its 60 changed images differ only in Dbgi. The integrated
compiler adds the related register and proper-list changes already
measured in Part 1868.

Compared with Part 1865, start_link/1 gains normalized equality.
Two snmpa_trap shell functions lose equality when the narrower move
rule retains a redundant temporary chain. A new pure-tail copy
composition preserves the final argument mapping and will be tested
with these two and the six restored controls. Nineteen of twenty
farther frame records return exactly to Part 1862 allocation widths.
The remaining generated parser frame needs the late operand rewrite
test. The complete release and managed gates remain pending.

[Scope](pilot.json) · [Focused tests](focused.json)
· [Raw section delta](compare-1865/pilot-section-delta.json)
· [Exact build maps](effective-snmp-maps-retake.json)
· [Normalized losses](compare-1865/lost-bodies.terms)
· [Frame origin census](compare-1865/frame-origins-retake.terms).

