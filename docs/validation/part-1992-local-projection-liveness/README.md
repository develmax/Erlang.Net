# Part 1992 local projection liveness checkpoint

Pin Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The accumulated [ledger](../part-1990-local-parity/observed-status.json) remains 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected.

| Scope | Different | Changed images prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: |
| [Projected Y homes and list results](p138-projected-y-roots-and-list-results/status.json) | 72 | 16 | 8 / 5 | 149.296 s |
| [Compact projections and known nil return](p140-compact-projections-and-nil-return/status.json) | 72 | 10 | 7 / 2 | 154.478 s |
| [Disjoint projection registers](p142-disjoint-projection-registers/status.json) | 28 | 3 | 3 / 0 | 73.159 s |

Each scope has zero Equal, Unsupported and SourceRejected. The 72-file attempts preserve all normalized losses. The final run covers those affected files and restores all observed losses. The frame is opened with the original live inputs. Constructed call arguments read a saved formal from its Y home only when an X destination needs that register. The available-copy set and projected destinations keep separate identities. In the faulty attempt the second field displaced the first in the same register. The final paired case supplies distinguishable values and compares both fields in the result. The nil path reuses X0 only when the source partition proves the empty list on the successful edge.

Every changed release image in the final result prepared in OTP. The three managed focused cases and 95 paired native calls passed. The [paired output](native-fixture.terms) has SHA-256 F0DEBBE8BA02D81E5F669CDB3CA4B369B0CC7A93104156B467B7425B754C02FF. The local worktree receives only the verified [source delta](source-transfer-2142.json). Fresh whole-corpus acceptance, complete regression, readiness and commit remain pending.
