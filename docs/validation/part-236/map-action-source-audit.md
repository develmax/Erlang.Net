# Inherited match-spec map-action ordering failure

Read-only review against OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. No production, fixture, harness, or expectation changes; no build or native execution performed for this review.

## Actual evidence

`part236-map-action-order-evidence.json` preserves the complete selected rows from immutable part234 and the failed part236 report. The old and new Source, SourceSha256, and CSharpSha256 are identical. The fixture independently expects `{ok,done}`. Previous original execution satisfied the assertion `{ok,first,[],[]}`; the current original result is `{error,error,{badmatch,{ok,second,[],[]}}}`. The SDK still produces `{ok,done}`. This is a real failed differential contract, not grounds to broaden accepted outcomes or exclude the case.

The source is:

```erlang
-module(match_spec_contract). -export([run/0]).
run()->{ok,first,[],[]}=erlang:match_spec_test([],
    [{'_',[],[#{a=>{message,first},b=>{message,second}}]}],trace),done.
```

## Source-confirmed mechanism

* `erts/emulator/beam/erl_utils.h:267-273`: `erts_cmp_flatmap_keys` uses `key_a - key_b` when both keys are atoms, and otherwise `erts_cmp(key_a,key_b,1,0)`. The comment explicitly restricts this comparator to the **internal** flatmap key order. This is not lexical Erlang atom term ordering.
* `erl_term.h` constructs an atom Eterm by shifting its table index and adding a common tag. `atom.c:330-345` obtains that index under the atom table write lock and constructs the atom. `index.c:index_put_entry` assigns a new entry the current entry count and increments it. Therefore relative internal atom-key order depends on prior atom registration.
* `erl_map.c:3187-3215`: flatmap validation sorts keys with the internal comparator and moves their values together. `utils.c:1561` explicitly says atom keys are sorted by atom index, not term order.
* `erl_db_util.c:4411-4466`: a flatmap body compiles its values through `dmc_array`. That function at 4325-4349 visits the last element first. For the two message actions, the last executed action is consequently the value associated with the **smallest internal key**.

Thus `a` need not precede `b` in the native flatmap. The two observed runs are consistent with a different relative registration history of these atoms. Neither artifact captures their actual indices; the precise registration producer is not established. Identical OTP source and identical fixture source do not imply identical native atom-table history. No claim of a native compiler defect follows from this failure.

## Actual SDK discrepancy

`src/Erlang.Terms/MapTerm.cs` orders Entries by `TermOrder.Compare(...,true)`. `TermOrder.cs:47-48` compares atoms by name codepoints. `MatchSpecCompiler.cs` then validates map values and keys in reverse Entries order. `MatchSpecProgram.cs:231-243` evaluates values and keys backwards in the same lexical order. The existing implementation accurately models reverse evaluation but starts with the wrong internal ordering for atom-key flatmap actions.

`AtomNameRegistry.IndexOf` already exposes runtime registration indices, and native record code uses an atom-index comparator. Its indices are CLR-runtime indices; they must never be described as native physical atom indices or assumed equal to an independently started OTP process.

## Proposed bounded correction and independent probes

1. In a new isolated candidate only, add a semantic match-spec flatmap key comparator: two direct Atom keys compare their `AtomNameRegistry.IndexOf` values, all other keys use exact `TermOrder.Compare`. Use this internal ordering consistently in compiler map-expression validation and runtime map-expression evaluation. Preserve the public MapTerm ordering and public atom term ordering. Review supported pattern-map traversal too, because its diagnostic order currently starts from lexical Entries. The current constructed-map cap is 32; this proposal does not establish HAMT traversal parity beyond that limit.
2. Explicitly correct the inherited fixture source to use integer keys `1` and `2` in place of `a` and `b`, retaining its old name, independent `{ok,done}`, context, policy, and streams. Numeric small keys use the exact numeric comparator, so backwards values execute `second` then `first`, independently of atom registration. Record old/new full Source, SHA256 and generated C# hashes. This is a semantic repair to a context-dependent source assertion, not a relaxed outcome.
3. Add a reversed source-insertion numeric case `#{2=>{message,second},1=>{message,first}}`, independently expecting `{ok,first,[],[]}`; construction order must not change numeric key sorting.
4. Add three numeric keys `#{3=>{message,third},1=>{message,first},2=>{message,second}}`, again expecting first, proving reverse sorted traversal beyond a two-element coincidence.
5. Add mixed keys `#{1=>{message,first},some_atom=>{message,second}}`, expecting first since numbers precede atoms under the exact comparator.
6. Add dynamically created, unique atom names in deliberately reversed lexical order. For example first create K1 from string `part237_match_map_z_first`, then K2 from `part237_match_map_a_second`, using `list_to_atom`, with neither atom written as an atom literal in the module. Assert both names were absent before creation, then map K1 to `{message,first}`, K2 to `{message,second}` and independently require first. Register a second unique pair in the opposite order and assign actions so the earlier-created key again wins. These controlled fresh-name probes expose the lexical/internal distinction without assuming the VM startup indices of `a` and `b`.
7. Add a guard diagnostic ordering probe with numeric map keys and unbound values, deriving the expected reverse-order diagnostics from `dmc_array`. A fresh-name atom counterpart may test the same controlled registration order. Keep diagnostic order exact and never allow both permutations.

The dynamic-name probes require actual source support for `list_to_existing_atom` absence and `list_to_atom` creation, and a harness context that has not already created the chosen unique names. Their complete bodies and expected native assertions must be audited before implementation. No new probe was executed here. No change to global map ordering, atom-table reset, fixture sorting, native report normalization, or exclusions is proposed.

## Reference notices

Pinned source is Apache-2.0. Relevant original notices: erl_db_util.c Ericsson AB 1998-2025; erl_map.c and erl_map.h Ericsson AB 2014-2025; erl_utils.h Ericsson AB 2012-2025; utils.c Ericsson AB 1996-2026; erl_term.h Ericsson AB 2000-2026; atom.c and index.c Ericsson AB 1996-2025. The raw pinned source snapshots in this artifact directory retain original evidence; no production source banners or private license file were added.
