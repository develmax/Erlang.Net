# Pinned OTP queue accounting prerequisite

The completed part 278 run captures queue memory accounting, not only emitted wire bytes. All measurements remain in the full typed/raw proof. This note derives an implementation prerequisite; it does not claim SDK allocator or ABI parity.

| Fresh scenario | Native queued qsize | Drained wire bytes | Difference | Returned segments |
|---|---:|---:|---:|---:|
| small | 238 | 78 | 160 | 3 |
| large_binary | 65808 | 65624 | 184 | 4 |
| two_binaries | 16709 | 16477 | 232 | 6 |

Pinned `dist.c:1255-1270`, `size_obuf`, counts `sizeof(ErtsDistOutputBuf) + sizeof(ErlIOVec) + eiov.size + vlen * (sizeof(SysIOVec) + sizeof(ErlDrvBinary*))`. Enqueue adds this size (`dist.c:3641-3648`). `dist_get_stat` reads `total_qsize` (`dist.c:4508`), so returning payload/wire length or an unconditional zero would be incorrect.

For the observed optimized Unix x64 build, the source layout is consistent with 32-byte `ErtsDistOutputBuf` (three pointers plus int/padding), 32-byte `ErlIOVec` (int/padding, size, two pointers), 16-byte SysIOVec and 8-byte binary pointers. The source reserves empty `iov[0]`; returned segments iterate indexes 1..vlen-1 (`dist.c:4669-4707`). Consequently `64 + 24 * (returnedSegments + 1)` reproduces 160, 184 and 232. These conditional sizes are source-derived, not a compiled offsetof/sizeof attestation. The additive VM profiles independently contain wordsize8, opt and x86_64-pc-linux-gnu; the three original sample VMs use the same installed attested binary but have no separate wordsize artifact.

Two-buffer output explains the apparent different small-buffer cost without inventing a special queue correction. qsize 66056 drops to 65810 after the first drain: 246 removed. Its wire is 86 bytes, with three segments, hence 86+160=246. The separate small sample is 78+160=238. The first control segment is 73 bytes rather than 65 because the additive probe uses longer node/name atoms; the extra 8 is actual wire length. The remaining second buffer is 65626+184=65810, then drops to zero with out=2. These are separate scenario identities, not interchangeable fixture constants.

`alloc_dist_obufs` (`dist.c:1192-1234`) allocates the backing driver binary and output/vector structures; `erts_ttb_iov_size` (`external.c:2399`) also includes the fragment pointer array. `size_obuf` is its own accounting function and is not the complete malloc footprint or Erlang process heap usage. Refcounted binary references, allocator headers and the fragment-pointer allocation must not be silently substituted into qsize. At `dist_ctrl_get_data`, wrapping returned segments allocates `2 + ERL_REFC_BITS_SIZE` heap words per segment, plus the get_size tuple (`dist.c:4674-4699`); these receiver heap objects are not retained output-queue qsize.

Finalization can change representation for pending hopeful data, atom-cache headers or fragments. `external.c:483` checks these layouts. For this observed pass-through configuration its PASS_THROUGH branch returns without rewriting the header; after retrieval, `dist.c:4707-4720` subtracts the actual finalized output buffer accounting. A future implementation needs the genuine segmentation/buffer lifecycle and finalized size transitions, not a constant wire overhead generalized to all protocols.

Counts and lifecycle remain separate: valid input moves in to1; malformed input returns ok and the controller immediately records in1, then it is killed and the owner reads reset zero after teardown. A retained handle works with zero stat after close but becomes badarg after reopening the next generation. None of these observations authorizes normalizing old native outcomes or reporting fabricated queue values in the SDK.
