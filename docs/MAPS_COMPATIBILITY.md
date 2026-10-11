# Maps API and compatibility scope

Baseline **OTP-29.1.1**, ad05823719d77c8faee87348ea39513d4e2f99c5. All **34 exported functions/arities** are registered and directly tested. Verified implementation **6363039e77a5eb1a15c2ea06b30f77e2fb267023**: [1626/1626 original comparison and 1869/full same-head CI](validation/part-108/remote-checkpoint.json). [90 new independent functional cases](validation/part-108/contract-matrix.json) plus preserved prior corpus cover ordinary operations, combining/grouping/error/callback/exact-key/iterator contracts. No production BEAM or new dependency.

| Export | Permanent direct contract |
| --- | --- |
| `filter/2` | `mfa/maps:filter/2` |
| `filtermap/2` | `mfa/maps:filtermap/2` |
| `find/2` | `mfa/maps:find/2` |
| `fold/3` | `mfa/maps:fold/3` |
| `foreach/2` | `mfa/maps:foreach/2` |
| `from_keys/2` | `mfa/maps:from_keys/2` |
| `from_list/1` | `mfa/maps:from_list/1` |
| `get/2` | `mfa/maps:get/2` |
| `get/3` | `mfa/maps:get/3` |
| `groups_from_list/2` | `mfa/maps:groups_from_list/2` |
| `groups_from_list/3` | `mfa/maps:groups_from_list/3` |
| `intersect/2` | `mfa/maps:intersect/2` |
| `intersect_with/3` | `mfa/maps:intersect_with/3` |
| `is_iterator_valid/1` | `mfa/maps:is_iterator_valid/1` |
| `is_key/2` | `mfa/maps:is_key/2` |
| `iterator/1` | `mfa/maps:iterator/1` |
| `iterator/2` | `mfa/maps:iterator/2` |
| `keys/1` | `mfa/maps:keys/1` |
| `map/2` | `mfa/maps:map/2` |
| `merge/2` | `mfa/maps:merge/2` |
| `merge_with/3` | `mfa/maps:merge_with/3` |
| `new/0` | `mfa/maps:new/0` |
| `next/1` | `mfa/maps:next/1` |
| `put/3` | `mfa/maps:put/3` |
| `remove/2` | `mfa/maps:remove/2` |
| `size/1` | `mfa/maps:size/1` |
| `take/2` | `mfa/maps:take/2` |
| `to_list/1` | `mfa/maps:to_list/1` |
| `update/3` | `mfa/maps:update/3` |
| `update_with/3` | `mfa/maps:update_with/3` |
| `update_with/4` | `mfa/maps:update_with/4` |
| `values/1` | `mfa/maps:values/1` |
| `with/2` | `mfa/maps:with/2` |
| `without/2` | `mfa/maps:without/2` |

## Functional behavior

intersect retains the second map's values. intersect_with and merge_with always pass key, original first-map value and original second-map value to the combiner, even when smaller-map traversal swaps the inputs internally. Only common keys invoke the callback. Arity validation precedes badmap errors; valid callbacks expose the first invalid map before the second. Results and input terms remain immutable with exact Erlang keys, including distinct numeric and composite keys.

groups_from_list validates the complete proper list before any callback. Callbacks traverse right-to-left; each key callback precedes its value callback, while each output group preserves input order. Callback error, throw and exit propagate. to_list accepts a map, ordered/reversed/custom iterator, tuple continuation or none; malformed iterators raise badmap carrying the original argument. Default traversal is unspecified, but keys, values, to_list, default iterator and map comprehension are consistent within each implementation.

next constructs immutable tuple chains and validates ordered keys/proper tails before returning, so missing later keys can reject a batch before callbacks. is_iterator_valid follows those chains. The initial first-key-only assumption and failed source are preserved in part106; the source-directed correction and five regression cases are recorded in part107.

## Boundaries

Exported API completion and functional proof do not establish byte-identical native HAMT/opaque iterator representation, allocation identity, native reduction/resource limits, full stack/error_info or entire upstream Common Test infrastructure. These require broader runtime/compiler work. Module status therefore remains **Partially compatible** despite **100% API presence**. No measured BEAM speed claim. All 90 new cases use default original optimizations; two prior map fixtures explicitly use no_ssa_opt for the pinned optimizer's map-only type approximation. Their original failures and source-directed record correction remain immutable 102/103/104 history; no new optimization workaround was introduced; part107 corrected one provisional source assertion based on the retained original failure in part106.
