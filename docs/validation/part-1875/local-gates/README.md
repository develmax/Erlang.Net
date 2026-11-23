# Part 1875 complete local validation

The full Windows release comparison uses the 221-path source snapshot
and pinned Windows OTP 29.1.1 provider. It reports 410 Equal, 956
Different, 0 Unsupported and 0 SourceRejected after 525.42 seconds.
Every release path is present and the compiler results are uncached.

| Gate | Verified result | Seconds |
| --- | --- | ---: |
| Standard solution build | Success on the verified source overlay | 214.00 |
| Tooling | Layout, examples, preprocessing-disabled and unavailable-oracle controls pass | 227.60 |
| Native backend | Console gate passes | 1.96 |
| Whole managed regression | 9,830 of 9,830 complete cases pass | 2630.49 |
| Libraries | Standard local library validation passes | 400.35 |
| RuntimeEvidence | Original comparisons and readiness pass | 61.94 |
| Catalog | Source and cold configured catalog, async observation and negative fences pass | 203.15 |
| Packages | Standard local package validation passes | 292.67 |

The source and cold Catalog runs each prepare all 1,321 admitted
images and agree on nine native observations, own-state map behavior
and cleanup. The cold package run also checks 176 captures. The
configured async route compares the original execution and checks the
physical negative fences. Managed and packaged readiness output match
on the full docs tree. The API delta is zero for all 21 registered
modules with 733 registered MFAs. The comparison report is the
immutable Part 1839 passing snapshot. Compiler and BEAM source build
status remains partially compatible.

The first RuntimeEvidence run reaches readiness and rejects the four
previous source digests. Each exact base blob and new source is
reviewed before rebinding. The list of verified entries and existing
classifications stays fixed. The isolated readiness render omits the
optional native inventory link because the staged docs intentionally
contain only the ledger inputs. Both normal and installed CLI renders
from full docs agree byte for byte and are the canonical outputs.

The first Catalog sees the Windows Store Python shim. The first
Packages attempt encounters full C storage during the cold consumer
restore. Installed Python 3.12 and a separately staged artifact volume
remove these environment constraints. Every transferred file is hashed.
The first subpath probe through the reparse point fails on DriverHost
initialization. The same 22 package files and validator, reached through
the physical path, pass all catalog observations and cleanup. The async
collector still requires a physical directory below the worktree. The
final standard Catalog run uses a physical repository artifact root
with physical native lanes and preserves historical outputs through
linked subdirectories. These are environment changes only.

[Full regression](repository-full.json.gz) ·
[Final input receipt](final-input-receipt.json) ·
[Binding audit](readiness-source-binding-audit.json) ·
[Initial gates](initial/aggregate.json) ·
[Retake gates](retake/aggregate.json) ·
[Final Catalog](catalog/Catalog.receipt.json) ·
[Whole release comparison](../full-uncached/README.md).


The tracked readiness JSON uses LF to match repository history. The
two official generated JSON files are byte-identical with Windows
line endings. [Raw JSON](generated-module-readiness.json.gz) ·
[Line-ending receipt](tracked-readiness-normalization.json).
