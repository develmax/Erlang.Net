# Table differences in the completed Part 1839 corpus

The inventory reads the 962 Different images and their release pairs.
The masks compare raw table entries. Imports are compared by resolved
atom names and arity. Literals are compared by ETF bytes with their
original stored ordering. A multiset permutation has no missing or
extra indexed entry.

| Dependency priority | Evidence | Modules | Interpretation |
| --- | --- | ---: | --- |
| 1 | Code chunk differs | 962 | The first differing opcode and allocation ranking identifies the frame and match-lowering groups. |
| 1 | Type table is a strict subset of the release entries | 860 | Missing SSA type facts and specialized typed operand emission. High confidence in the missing entries. |
| 2 | Different atom membership, with 101 additional order-only cases | 824 | Code order, helper reachability and operation selection precede atom renumbering. |
| 2 | Different resolved imports, with 86 additional order-only cases | 719 | Ordinary calls, BIF rewrites and dead branches alter actual imports. Ninety-four additional raw differences arise with equal ordered resolved imports. |
| 3 | Different literal membership, with 142 additional order-only cases | 565 | A prior constructor, call or match lowering determines most literal entries and uses. |
| 3 | Line chunk differs | 926 | Framing and physical branch order alter the encounter sequence. The inventory measures the chunk deltas. |

The independent literal-order replay visits literal instructions in
their physical order using the pinned opcode map. It decodes all 1,366
reference images and leaves their literal order unchanged. Simulated
reindexing changes 23 generated images and restores four LitT chunks.
It restores zero Code chunks or whole files. No output from this
experiment enters the compiler or acceptance comparison.

[Module groups](resolved-table-groups.json) ·
[Literal replay](offline-litt-replay.json) ·
[Inventory script](beam_chunk_inventory.py) ·
[Replay script](replay_literal_order.py) ·
[Ranked function groups](../part-1832/README.md).

The Type membership audit further isolates bounded integer entries
missing from 495 modules, proper-list entries missing from 467,
and atom-or-tuple entries missing from 404. The 495-module group
contains 6,397 missing bounded integer entries. These entry counts
can overlap within the same function or module. Rank producer and
operand sites before choosing the type pass to port.

[Type counts](type-entry-groups.json) · [Type census](type-entry-groups.py).
