# Final ordinary-call argument scheduling

## Evidence from Part1973

Pinned OTP is ad05823719d77c8faee87348ea39513d4e2f99c5. The immutable Part1973 input has 509 Equal and 857 Different, compiler SHA-256 f685472e21eddb969dc244fa620539ff5d145c8d267b0f8e9486a7f30dad2476. The candidate statistics use the actual generated images for Different rows and byte-identical pinned representatives for Equal rows.

| Physical window | Sites | Modules |
| --- | ---: | ---: |
| Disjoint stack source moved into X0 before the contiguous kills, Different | 1,301 | 361 |
| Same window, Equal | 0 | 0 |
| X0 source also killed, Equal | 265 | 100 |
| Independent move already after the kills, Equal | 46 | 11 |
| Exact inverse pair at the same ordinary-call ordinal in sources with matching call sequences | 57 | 45 |
| Exact pairs in ordinary functions | 54 | 43 |
| Exact pairs in functions with receive | 3 | 3 |
| Other pinned reordered pairs with a different stack slot or kill set | 182 | 104 |

The 54 eligible exact pairs have 28 local and 26 external calls. Between the candidate and pinned code, the pairs agree on the target, physical source Y and contiguous kill groups. A diagnostic replay across the 45 paired modules matches all typed and line-annotated instructions in one additional function, `erl_prettypr:lay_1/2`. Unix's two arity-six sites are in functions whose loops and errors outside those windows differ. No module is wholly equal by this diagnostic. Raw bytes are the acceptance gate.

The full 1,301-site disassembly census finds 1,288 ordinary sites and 13 in receive functions. No candidate site lies in a function containing explicit try/catch or executable/debug line instructions. The exact pair scope has three receive controls in `ct_netconfc:call/4`, `megaco_digit_map:collect/5` and `mnesia_recover:call/1`. No exact pair targets a recognized direct exception or trap BIF. The wider census has one potential indirect trap at `timer:tc/4` through `erlang:apply/3`. It has a different pinned window and is a behavioral control. An ordinary local or external call can propagate an error or trap. Both remain at the same opcode with the same arguments, stack slots and call line.

## Source rule and final placement

Pinned `lib/compiler/src/beam_ssa_codegen.erl` lines 2653–2685 implement `setup_args/4` with context `none`, then `opt_move_to_x0` and `move_past_kill`. The latter crosses each `init_yregs` only if the move source is outside every kill group. Other contexts emit argument setup without this kill scheduling.

The proposed late replay invokes the existing dependency-aware helper directly after `OrderFinalPhysicalLabels` and before `OrderRelocatedEntryTables` and `CompactFinalAtoms`. This follows `ClearConsumedProjectedCallArguments`, `ClearConsumedOutgoingRoots` and its trim, `ClearDeadProjectedBranchRoots`, and the final label pass that can run `ClearOverwrittenOutgoingRoots` and `FinalizeSharedFrameExits`. Several of those operations emit or place root initializations. The two Unix windows are independently proven to be present in the final Part1973 `Code` and accepted by the existing reorder proof on those exact bytes. The first writer of each Unix kill group has not been isolated. The current early helper remains responsible for trims and Y relocation.

The final mode accepts a bare untyped Y-to-X0 move followed immediately by complete Y initialization groups, then optional ordinary source line annotations and a positive-arity static local or external call. The existing dependency check protects every slot read. A GC, branch, dynamic call or other instruction ends the pattern before a match. The function-wide exclusion covers try/catch regions including physically separated handlers, receive instructions and executable/debug line markers. This conservative guard avoids needing a CFG handler and receive-state reconstruction. The transformation changes neither frame size nor roots at the call. There is no safepoint or executable line between the moved instruction and the kills. It preserves the original instruction encodings, labels and total byte length. It runs under the existing optimization and SSA-type-inference guards.

The raw replay of the existing frozen implementation found the Unix source lines 128 and 144. Their final `Code` offsets are 188 and 430, with Y6 disjoint from kills [Y2,Y3,Y4] and Y7 from [Y3]. This uses the generated Part1973 bytes for proof only.

## Qualification

1. Stage the strict compiler and permanent regression patches on the exact package preimages. Build and run `compiler/beam/orders-late-argument-zero-after-independent-frame-kills`, plus the existing early argument/trim tests. The new test checks full instruction bytes for local and external calls, call Line, multiple kills and idempotence, with source-killed, typed, literal, GC, zero-arity, protected and debug negatives.
2. Compile the 45 physical-pair modules, four immutable Equal controls and `timer`. Use `scope-modules.txt` and verify the baseline rows and hashes in `scope-identities.tsv`. The four Equal controls have actual source-killed or already ordered cases. Keep the three receive functions unchanged in ABI-scheduling checks. Check raw output and status separately from normalized code for all modules. Inspect every changed release image. No raw acceptance is recorded here.
3. Load actual generated outputs in separate pinned native processes. The saved `run-native-controls.ps1` accepts `-Erl`, `-TimerBeam`, `-PrettyBeam` and `-Log` in that order. It checks the completed return value of indirect apply, a long list traversal that can yield, an error's original timer line, and formatting a syntax node with comments. The pinned and frozen Part1973 native logs agree. Repeat with the actual built candidate and compare both logs. Exact raw comparison remains a separate gate.

