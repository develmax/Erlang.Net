# Producer binary results on failure edges

This is an independent local compiler experiment. The oracle is the installed Windows OTP 29.1.1 release and the pinned source tag resolving to ad05823719d77c8faee87348ea39513d4e2f99c5. The candidate source base is the frozen T2 archive DE7C26FBC23B2B18C731D813F8096270049D36DBA57740A651C979C5D83E59F9 combined with unchanged compiler files at the local tracked archive 1a5d3057eddddffdd8b9e0e8dfd6655d0a9df9d2. The C# compiler source is isolated. The repository main worktrees and the external temporary extraction are not used by the experiment.

In ssh_bits:mpint/1 the native compiler keeps the call result in X0 through bs_start_match3 and the failing prefix check. The terminal ensured integer extraction can then overwrite X0 on success. C# saves the result to Y, restores it before badmatch and acquires a redundant frame. Retention of the original X0 is valid even for non-binary and unaligned producer results. It does not assume the behavior of a named external function.

Pinned beam_ssa_pre_codegen bs_combine combines extraction with an ensured get. The x86 ERTS instr_bs.cpp emits the ensure-at-least failure before the integer result is written. More general packed match sequences also preserve X0 when they neither assign it nor exclude it from a GC live prefix. The candidate recognizes only branches whose two direct predecessor failures have exactly that ownership. The saved slot is initialized before the removed write, and the success path never reads it.

The S release-wide diagnostic census found two eligible sites in ssh_bits:mpint/1 and 14 potentially eligible functions over twelve other modules. The wider predicate is still a disassembly estimate pending actual source compilation. Candidate pilot inputs include ssh_bits, an independently shaped DETS reader, a binary parser and two exact controls. No parallel compiler worker runs. The candidate pilot waits for the case-ownership cohort's scope receipt.

Pinned ERTS prepared both original ssh_bits and baseline S's different image and produced identical 34-case transcript D692C24677C099D817E83A11696805044C224F714FA8BACF61AFBCE22B949BBE. The synthetic witness additionally compares successful bodies, short, unaligned and non-binary failures and a function that retains the original term. This verification is separate from release byte equality. Remaining SSH deltas include negative-branch mask construction scheduling, range and context types, and layout of binary failure blocks.

## Intermediate retake and pass schedule

The first pilot removed two stack moves in the positive mpint branch, dropped its allocation width from one to zero, restored the byte range on the following bitwise BIF and exposed the existing cold-failure move. The synthetic fixture retained a redundant producer copy because two success-only register transfers assigned its slot before the join. The warm-transfer retake handles that distinct definition and proved both options. Its permanent fixture passed. ERTS ran 18 synthetic and 34 real cases with full transcript equality. The 16-module raw result was 2 Equal and 14 Different with no changed acceptance status.

The warm-transfer retake ran too early in the optimizer's local fixed point. It erased the precondition of RetainPositionedBinaryTail and left three warm frame slots in the positive SSH branch. That diagnostic report is retained in ../producer-binary-match-retake-1917. The current pipeline runs the existing position-aware proof first. A sized synthetic continuation now checks the retained tail separately and the real SSH gate checks both direct producer matches plus the zero-width positive frame. These sharper checks are prerequisites for the 16-module retake.

## Completed fixed-point retake

The late phase runs only after the existing fixed-point rewrites have exhausted the current code. This keeps the earlier positioned-tail and frame canonicalizations eligible. The expanded permanent fixture failed on T2 and passes this candidate, including separately checking the retained tail used by length calculations. All 27 synthetic and 34 actual ERTS observations match and record values, errors and stack source intervals.

Both source-derived ssh_bits producer matches now use the original X0 directly on rejection. The positive branch allocates a zero-width frame and keeps the positioned binary tail. Its binary construction remains register-based. Across the module, the raw Code size changes from 627 to 609 bytes against a 623-byte reference, and the Type section changes from 48 to 66 bytes against a 143-byte reference. These measurements record bytecode changes. The whole module remains Different.

The fresh 16-module comparison took 54.96 seconds and retained 2 Equal and 14 Different, with zero Unsupported and zero SourceRejected. Seven images and nine named function instruction bodies changed against T2. No normalized function or full exact match was gained or lost. ERTS accepted 18 preparation attempts. Reference, before and after images of crypto uniformly return on_load_not_allowed from code:prepare_loading. Its raw messages are preserved. The affected group includes source-derived match failure reductions in crypto, pubkey_pbe, public_key, ssh_bits, ssh_transport, tls_server_session_ticket and dets_utils.

- Compiler SHA-256 F4AF9322B7D89082C52105749B4A2C2AEF95440278664B7F3F8A4220DE510A2C
- Raw 16-path report scope.json SHA-256 D8C566C151F8B2B58ABFBEEC78F54C4DD04F199B63599FE4CF659065C534BFBF
- Summary scope-summary.json SHA-256 F56FD039E2BC579A2D7CC99E6826A02458250D7D20C4AEC84AC62B9CA4131D85
- Native loader group-loader.terms SHA-256 117E063241869DAD02432DE22B6CBFD974AA67853347C5C8338722A6DE2D829B
- Normalized group-normalized.terms SHA-256 B9C11EFA26F66598F6A27AF5DD5B3CFC177008EA027F720AF80B6511EE385040
- Strict T2 patch reuse-producer-result-for-binary-failure.patch SHA-256 1649A9349BDB18DB562276666E4966DEDB88911E36951AB8A99E33B7C1BDC3E8
- Strict replay against U reuse-producer-result-for-binary-failure-u.patch SHA-256 D5BF734F61F083DF0C4972326308620C40F530A4DDDC1FBD9EB91F8FBE6B024E

Remaining ssh_bits groups include negative-branch frame ownership, variable-width binary extraction, type annotations on binary construction operands, and ordering of the nested match failures.
