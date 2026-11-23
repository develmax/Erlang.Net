# Retained tuple field scheduling scoped verification

Pinned OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Final integration patch SHA-256 7F94FB91EF2C7C63E828A9BC816DA7BA799265EE3D7FF3B44F8C84F3E6E1DF19. Candidate compiler SHA-256 2178EFD7EF50CCAC8906B80FA34CB3CC86C3E0D052F5665D21A72F8F9BB80655. Baseline compiler SHA-256 B6C017C1AFEAFC788C0BFA407A366C8CD1E4082D558EE02B0218C0106A499609. See receipt.json for tool, selection and fixture identities.

| Nine-image single-worker uncached scope | Before | After |
|---|---:|---:|
| Equal | 2 | 2 |
| Different | 7 | 7 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Comparison time | 5.38 s | 5.64 s |

No whole-image gain or loss. Exactly five release modules changed, and their module/2 functions became newly equal after normalized instruction comparison against OTP. These are beam_a, beam_block, beam_flatten, beam_trim and beam_z. No normalized losses and no changed non-target bodies. Sections changed are Attr and Code in all five plus FunT in four. All five have unchanged FunT identity shape. The two related differing controls and two exact controls remained byte-stable.

The compiler and miniature test built without warnings or errors. The permanent test is red on the frozen compiler specifically on the pack/2 schedule and green on the candidate. It checks three precise schedules, the no_ssa_opt opt-out and seven interpreted outcomes. ERTS prepared all five changed release images and loaded and executed their actual module/2 exports over the same source-generated module code as the pinned oracle. It separately loaded and executed both fixture images with eight matching outcomes.

The initial isolated build took 76.89 s and is retained in verify-1. The first test harness attempted the public source mapper with no_ssa_opt and hit its established rejection of this -compile attribute; verify-1/mini-after.log preserves the exception. The corrected test follows the existing suite's direct parsed-module emission path for no_ssa_opt. The production source and compiler binary stayed fixed. The retake reused that compiler, rebuilt the test harness and ran red/green plus both nine-image comparisons in 29.74 s. Independent ERTS verification took 3.60 s. The SDK layout run on the new files reports zero files needing formatting.

No full corpus or shared integration build ran in this isolated scope.
