# Lists API and compatibility scope

Baseline **OTP-29.1.1**, ad05823719d77c8faee87348ea39513d4e2f99c5. All **91 exported functions/arities** are registered and directly tested. Verified implementation **9aeb8f1c6943dad8bbba22689e3de95e48a6d338**: [1878/1878 original comparison and 2181/full same-head CI](validation/part-110/remote-checkpoint.json). [252 new independent functional modules](validation/part-110/contract-matrix.json) supplement the preserved prior corpus. No production BEAM or new dependency.

| Export | Permanent direct contract |
| --- | --- |
| `all/2` | `mfa/lists:all/2` |
| `any/2` | `mfa/lists:any/2` |
| `append/1` | `mfa/lists:append/1` |
| `append/2` | `mfa/lists:append/2` |
| `concat/1` | `mfa/lists:concat/1` |
| `delete/2` | `mfa/lists:delete/2` |
| `droplast/1` | `mfa/lists:droplast/1` |
| `dropwhile/2` | `mfa/lists:dropwhile/2` |
| `duplicate/2` | `mfa/lists:duplicate/2` |
| `enumerate/1` | `mfa/lists:enumerate/1` |
| `enumerate/2` | `mfa/lists:enumerate/2` |
| `enumerate/3` | `mfa/lists:enumerate/3` |
| `filter/2` | `mfa/lists:filter/2` |
| `filtermap/2` | `mfa/lists:filtermap/2` |
| `flatlength/1` | `mfa/lists:flatlength/1` |
| `flatmap/2` | `mfa/lists:flatmap/2` |
| `flatten/1` | `mfa/lists:flatten/1` |
| `flatten/2` | `mfa/lists:flatten/2` |
| `foldl/3` | `mfa/lists:foldl/3` |
| `foldr/3` | `mfa/lists:foldr/3` |
| `foreach/2` | `mfa/lists:foreach/2` |
| `join/2` | `mfa/lists:join/2` |
| `keydelete/3` | `mfa/lists:keydelete/3` |
| `keyfind/3` | `mfa/lists:keyfind/3` |
| `keymap/3` | `mfa/lists:keymap/3` |
| `keymember/3` | `mfa/lists:keymember/3` |
| `keymerge/3` | `mfa/lists:keymerge/3` |
| `keyreplace/4` | `mfa/lists:keyreplace/4` |
| `keysearch/3` | `mfa/lists:keysearch/3` |
| `keysort/2` | `mfa/lists:keysort/2` |
| `keystore/4` | `mfa/lists:keystore/4` |
| `keytake/3` | `mfa/lists:keytake/3` |
| `last/1` | `mfa/lists:last/1` |
| `map/2` | `mfa/lists:map/2` |
| `mapfoldl/3` | `mfa/lists:mapfoldl/3` |
| `mapfoldr/3` | `mfa/lists:mapfoldr/3` |
| `max/1` | `mfa/lists:max/1` |
| `member/2` | `mfa/lists:member/2` |
| `merge/1` | `mfa/lists:merge/1` |
| `merge/2` | `mfa/lists:merge/2` |
| `merge/3` | `mfa/lists:merge/3` |
| `merge3/3` | `mfa/lists:merge3/3` |
| `min/1` | `mfa/lists:min/1` |
| `nth/2` | `mfa/lists:nth/2` |
| `nthtail/2` | `mfa/lists:nthtail/2` |
| `partition/2` | `mfa/lists:partition/2` |
| `prefix/2` | `mfa/lists:prefix/2` |
| `reverse/1` | `mfa/lists:reverse/1` |
| `reverse/2` | `mfa/lists:reverse/2` |
| `rkeymerge/3` | `mfa/lists:rkeymerge/3` |
| `rmerge/2` | `mfa/lists:rmerge/2` |
| `rmerge/3` | `mfa/lists:rmerge/3` |
| `rmerge3/3` | `mfa/lists:rmerge3/3` |
| `rukeymerge/3` | `mfa/lists:rukeymerge/3` |
| `rumerge/2` | `mfa/lists:rumerge/2` |
| `rumerge/3` | `mfa/lists:rumerge/3` |
| `rumerge3/3` | `mfa/lists:rumerge3/3` |
| `search/2` | `mfa/lists:search/2` |
| `seq/2` | `mfa/lists:seq/2` |
| `seq/3` | `mfa/lists:seq/3` |
| `sort/1` | `mfa/lists:sort/1` |
| `sort/2` | `mfa/lists:sort/2` |
| `split/2` | `mfa/lists:split/2` |
| `splitwith/2` | `mfa/lists:splitwith/2` |
| `sublist/2` | `mfa/lists:sublist/2` |
| `sublist/3` | `mfa/lists:sublist/3` |
| `subtract/2` | `mfa/lists:subtract/2` |
| `suffix/2` | `mfa/lists:suffix/2` |
| `sum/1` | `mfa/lists:sum/1` |
| `takewhile/2` | `mfa/lists:takewhile/2` |
| `ukeymerge/3` | `mfa/lists:ukeymerge/3` |
| `ukeysort/2` | `mfa/lists:ukeysort/2` |
| `umerge/1` | `mfa/lists:umerge/1` |
| `umerge/2` | `mfa/lists:umerge/2` |
| `umerge/3` | `mfa/lists:umerge/3` |
| `umerge3/3` | `mfa/lists:umerge3/3` |
| `uniq/1` | `mfa/lists:uniq/1` |
| `uniq/2` | `mfa/lists:uniq/2` |
| `unzip/1` | `mfa/lists:unzip/1` |
| `unzip3/1` | `mfa/lists:unzip3/1` |
| `usort/1` | `mfa/lists:usort/1` |
| `usort/2` | `mfa/lists:usort/2` |
| `zf/2` | `mfa/lists:zf/2` |
| `zip/2` | `mfa/lists:zip/2` |
| `zip/3` | `mfa/lists:zip/3` |
| `zip3/3` | `mfa/lists:zip3/3` |
| `zip3/4` | `mfa/lists:zip3/4` |
| `zipwith/3` | `mfa/lists:zipwith/3` |
| `zipwith/4` | `mfa/lists:zipwith/4` |
| `zipwith3/4` | `mfa/lists:zipwith3/4` |
| `zipwith3/5` | `mfa/lists:zipwith3/5` |

## Functional behavior

Basic traversal, structural operations, sequence/sublist/enumeration, concatenation and exact subtraction preserve Erlang values and early tails. concat formats finite floats with the original legacy exponential representation with 20 fractional digits, including negative zero. Key operations search with numeric guard equality and skip non-tuples or short tuples; destructive-looking operations construct immutable results. uniq uses exact equality, including composite numeric terms; sort/usort/keysort/ukeysort use Erlang numeric ordering and stable first representatives.

zip/zip3 and zipwith/zipwith3 support fail, trim and pad; callbacks and invalid mode checks follow original clause order. One-remainder padding comprehension propagates bad_generator on an improper tail. Callback map/folds/filters/search/splitwith/flatmap follow original evaluation order and propagate error, throw and exit. flatmap evaluates callbacks before unwinding append checks. Callback sorting/unique merging follow native comparison directions/state transitions; ascending and reverse merges retain original tie choices. Lazy merge-many grouping and keyed sorting preserve nested fault precedence.

Permanent cases cover all direct exports, callbacks/empty/short/improper lists, badfun/badarity, numeric and composite keys, stable values, error ordering, literal comparison traces, zip padding and 64 deterministic sort/merge vectors. Final independent success values are literal contracts; no outcomes were adopted from execution. The source/reuse audit and initial corrected provisional assertions remain [immutable part 109](validation/part-109/reference-audit.json).

## Boundaries

**100% exported API presence** and verified functional scope do not establish identical native allocation identity, reduction/resource ceilings, full stack/error_info or exhaustive upstream Common Test infrastructure. Pure sorting uses stable BCL projection and three-way merging composes two-way value operations; internal recursion/allocation structure differs. Full runtime/compiler and performance work remains, and module status stays **Partially compatible**. No measured BEAM speed claim. All 252 new cases use default original optimization; two historical map fixtures with explicit no_ssa_opt stay unchanged.
