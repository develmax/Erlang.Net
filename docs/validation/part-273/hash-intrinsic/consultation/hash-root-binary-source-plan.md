# Root binary Portable2 source optimization proposal

Seed: 32ae9d43feb15314d2991161345cc4ba794f3ce4. Isolated candidate: artifacts/hash-root-binary-part259-prototype. One owned runtime path: src/Erlang.Runtime/ErlangNativeHash.cs. Source freeze: hash-root-binary-source-freeze.json, SHA256 97d4a40e1264d93d403d31f674abeda25f252ea1834d800a11b472eab473b59c.

The proposed early root BitString branch invokes BinaryHash with incoming hash zero before constructing Stack<Frame>. The existing nested binary case invokes exactly the same helper with its incoming traversal hash. The extracted computation preserves unchecked BinaryMix+hash seed, empty binary identity, block hashing of full bytes and BitTailMix of tail width/value. No source-language fixtures, independent expected hashes, registration or allocation budget change.

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 erl_term_hashing.c binary case around lines1260–1310 supplies the mathematical contract. Its pre-estack optimization around970 handles immediate atoms/integers; it does not contain a root binary pre-estack branch. The earlier delegation assumption about an OTP root binary fastpath was corrected. This is a C# traversal-allocation optimization, not a claim of copying that native control flow.

Before build: independent source peer verifies one-file before/after ledger, helper extraction and unchanged nested control flow. Hold parent Git HEAD stable while target build owns heavy slot.

Required actual focused checks after authorization:

- Bind old seed Runtime DLL and new Runtime DLL full SHA256/MVID; construct identical binary terms independently of candidate output. Compare both Portable2 delegates through separately loaded dependency contexts.
- Deterministic root vectors at 16 full-byte sizes (0,1,2,3,4,7,8,11,12,13,15,16,23,24,25,4096), each with tail widths0–7, plus 4MiB existing allocation input. Include all-zero and varied bytes. No output-derived expected baseline is accepted as native proof.
- Compare nested tuple/list/map binary terms so incoming nonzero traversal hash and map pair behavior remain exact. Root versus nested hash values need not be equal.
- Execute all unchanged independent ErlangHashCases existing vectors through interpreted and generated fixtures; preserve every independently declared Expected result and source identity. Historical original proof remains bound to its head, not relabeled as this change.
- Run unchanged ErlangHashHostChecks.Resources (4096-byte allocation budget) and deep tuple/map traversal. No budget relaxation or exception normalization.
- Warm both direct delegates before16 current-thread allocation samples; record same-thread and synchronous validity, actual per-call bytes and hash values. Potential reduction from152 to0 is a hypothesis until measured. Registry-call allocation includes separate tracing/argument/result costs.

No build, output comparison, allocation measurement or native execution has occurred for this candidate. No claim that this addresses the historical CI allocation outlier, increases whole-module compatibility, or is faster than native OTP is made.
