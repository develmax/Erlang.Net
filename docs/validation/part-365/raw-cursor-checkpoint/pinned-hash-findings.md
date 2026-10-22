# Pinned HAMT hash and cursor source findings

Read-only authority exported with git show from immutable OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5; exact blobs and hashes are retained in source-bindings.json. No original/SDK/native execution or implementation changes were performed.

## Hash helper reuse

erts_map_hash (erl_term_hashing.c2311) calls erts_internal_hash (2300), not make_hash2. Immediate terms use ihash_mix64(rawEterm); compound terms use make_internal_hash, an incremental64-bit MurmurHash3-derived128-bit state folded to the native word. The source1609 explicitly says it is not portable between VM instances and some hashes remain valid only while their ErlNode identity exists.

Existing managed ErlangNativeHash.Portable2 implements phash2's portable32-bit family; ErlangNativeHash.Atom is the atom-name hash and ErlangBlockHash is the Jenkins-style block mixer. None is an exact replacement for internal HAMT hash. Reuse term traversal/bit reading only after checking its order; do not call any existing helper as a silent fallback. Owner proplists_slice is preparing trie/path view and exact64-bit integer hashing separately; this review implements neither.

On64-bit OTP, make_small is (unsignedValue<<4)|15 for signed60-bit payload, and make_atom is (atomIndex<<6)|11. Thus the immediate fast path depends on actual VM atom-table indices, not atom names. Local pid/port words encode VM fields. External refs/pids/ports mix ErlNode pointers; external FUN mixes Export pointer. Local FUN mixes code-table index/oldunique plus tagged module atom and captured environment. Map-as-key hashing traverses actual internal flatmap/HAMT representation and iteration order, explicitly only stable for identical maps within a VM. A portable semantic term hash cannot manufacture these native identities or original layout.

Integer-only exact source port is feasible: reproduce tagged small60-bit and signed bignum limb64 representation and the original finalization; preserve negative big sign marker and number of limbs. That proves only this key domain until separately observed. Float uses bit pattern including native word halves; string optimization groups byte-list runs, and tuple/record/map order uses deferred stack markers. A generic full provider needs deliberately modeled per-runtime identities/layout, not a dictionary-order approximation. No such generic provider was found in managed source.

## Topology, collisions and cursor edges

Pinned erl_map.c3586 describes4-bit path elements, low nibble first:0x210 means compressed child slots0,1,2, then implicit zero slots until a leaf. These are occupied child-array ordinals, not raw hash bucket numbers (3830–3836). A leaf ends path decoding without inspecting unused high cursor bits.

Equal full-hash runs are sorted by ascending CMP_TERM (787–791, hxnodecmpkey1160), then collision nodes preserve that order (~875). merge_collision_node1528 confirms ascending comparison. Hash bits are consumed low nibble first during ordinary lookup; batch-build swizzle_map_hash4170 reverses nibble order for sorting and reconstructs the same trie. Exact full-hash collision is not merely equal low nibble. Collision iteration extends its chunk rather than yield mid-node (3905); idx!=0 at an entered collision is a debug assertion, not an ordinary production badarg check. Do not add a rejection inferred only from that assertion.

Flatmaps (<=32 release-build entries): path must be a SMALL whose unsigned_val<=map size (3718). Success returns the entire map; path is not an offset. Negative SMALL becomes a large unsigned payload and fails this bound. A numerically small value represented as a positive BIG is not admitted by this flatmap branch.

HAMT: negative BIG explicitly fails3804; SMALL of either sign is converted using unsigned_val3797, so do not blanket-reject negative SMALL. Its nibbles route until leaf or idx>=node size causes badarg. Positive BIG uses low64-bit limbs first and missing limbs zero-extend. High unused digits can be ignored if an earlier leaf is reached. Path0 limits initial chunk to32; later chunk size depends on reductions, but emitted association order is independent of reductions (3869). List mode traverses reversed slots and encodes offsets from end; iterator mode traverses forwards. The two protocols must not share an unqualified path decoder.

## Current decision

Root is clarifying public22 API/opaque iterator contracts versus manually forged raw internal HAMT cursors. No generic hash implementation is proposed or integrated pending that scope answer. Existing public iterator and small-map raw path corrections remain Root-owned. Do not fake original atom indices/pointers, claim portable internal cursor identity, or weaken an actual source branch silently. A new-only original HAMT witness is appropriate only after the intended source/provider model is reviewed; old sealed original groups need not be rebuilt.
