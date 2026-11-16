# All 71 string API contracts

Pinned OTP 29.1.1; source commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

Each row links a source clause review to actual complete outcomes for **each** selected backend.
Counts include repeated smoke rows between bounded chunks. They are observations, not unique inputs
or a proof over every possible term, host or resource limit. Compatibility statuses remain unchanged.

Returned `{error, Reason}` tuples count as returned values; exceptions require their class, reason and full stack.

| API | Source clause lines | Default arguments | Contract reviewed | Returned values / exceptions per backend |
| --- | --- | --- | --- | --- |
| `is_empty/1` | 236, 237, 238, 239 | explicit arguments | Only recursively empty lists/binaries are empty; other terms return false without Unicode validation. | 777 / 0 |
| `length/1` | 256, 258 | explicit arguments | Latin1 pair fast path excludes CR; count grapheme clusters, preserve malformed-tail helper errors. | 590 / 188 |
| `to_graphemes/1` | 276 | explicit arguments | Return cluster terms, not scalar strings; retain non-tail recursion source frames. | 590 / 188 |
| `reverse/1` | 363, 365 | explicit arguments | Reverse grapheme terms into a list even for binary input; do not reverse bytes or codepoints. | 590 / 188 |
| `equal/2` | 290, 292 | IgnoreCase=false, normalization=none | Binary equality shortcut, exact scalar comparison, identity shortcuts and dynamic normalization dispatch; boolean guard precedence. | 65 / 151 |
| `equal/3` | 304, 306 | normalization=none | Binary equality shortcut, exact scalar comparison, identity shortcuts and dynamic normalization dispatch; boolean guard precedence. | 65 / 403 |
| `equal/4` | 341, 343, 345 | explicit arguments | Binary equality shortcut, exact scalar comparison, identity shortcuts and dynamic normalization dispatch; boolean guard precedence. | 61 / 759 |
| `slice/2` | 376 | length=infinity | Zero length precedes index validation; nonnegative start and positive/infinity length; preserve binary/list result and lazy suffix. | 41 / 165 |
| `slice/3` | 403, 409, 414 | explicit arguments | Zero length precedes index validation; nonnegative start and positive/infinity length; preserve binary/list result and lazy suffix. | 1571 / 380 |
| `pad/2` | 426 | direction=trailing, character=space | Length is integer; direction checked before traversal; arbitrary fill term, zero/negative deficit and odd both-padding placement. | 45 / 171 |
| `pad/3` | 435 | character=space | Length is integer; direction checked before traversal; arbitrary fill term, zero/negative deficit and odd both-padding placement. | 47 / 421 |
| `pad/4` | 459, 462, 465 | explicit arguments | Length is integer; direction checked before traversal; arbitrary fill term, zero/negative deficit and odd both-padding placement. | 170 / 651 |
| `trim/1` | 480 | direction=both, pinned pattern whitespace | Empty separators bypass direction validation; pinned whitespace default; original list membership and binary/mixed suffix boundaries. | 22 / 41 |
| `trim/2` | 493 | pinned pattern whitespace | Empty separators bypass direction validation; pinned whitespace default; original list membership and binary/mixed suffix boundaries. | 24 / 191 |
| `trim/3` | 522, 523, 526, 528, 531, 534 | explicit arguments | Empty separators bypass direction validation; pinned whitespace default; original list membership and binary/mixed suffix boundaries. | 61 / 425 |
| `chomp/1` | 553 | explicit arguments | Trailing CRLF cluster or LF only; other whitespace retained; use original trim helper stack. | 461 / 40 |
| `take/2` | 564 | complement=false, direction=leading | Empty separator and direction/complement branch order; singleton versus multiple separators; cp_prefix exact continuation equality and lazy malformed suffix. | 63 / 153 |
| `take/3` | 574 | direction=leading | Empty separator and direction/complement branch order; singleton versus multiple separators; cp_prefix exact continuation equality and lazy malformed suffix. | 43 / 425 |
| `take/4` | 603, 611, 613, 616, 619 | explicit arguments | Empty separator and direction/complement branch order; singleton versus multiple separators; cp_prefix exact continuation equality and lazy malformed suffix. | 155 / 885 |
| `lexemes/2` | 965, 966, 967 | explicit arguments | Literal empty list ignores separator type; other inputs require list separators; omit empty lexemes and retain chardata representation. | 56 / 167 |
| `nth_lexeme/3` | 988, 989 | explicit arguments | N=1 and empty separators return original input; otherwise positive integer N and list separators, including huge absent indexes. | 46 / 423 |
| `uppercase/1` | 638, 642, 647, 649 | explicit arguments | Pinned Unicode mapping and expansion; Latin1 fast path and malformed binary/list source frames; preserve unchanged binary. | 590 / 188 |
| `lowercase/1` | 669, 673, 678, 680 | explicit arguments | Pinned Unicode mapping and expansion; Latin1 fast path and malformed binary/list source frames; preserve unchanged binary. | 590 / 188 |
| `titlecase/1` | 697, 702 | explicit arguments | Map first codepoint only and preserve unvisited suffix; input shape guard differs from full case conversion. | 599 / 179 |
| `casefold/1` | 725, 729, 734, 736 | explicit arguments | Pinned Unicode full casefold and expansions; no culture-sensitive BCL substitutions. | 590 / 188 |
| `prefix/2` | 845 | explicit arguments | Convert pattern first; converted empty returns input; malformed conversion tuple reaches prefix_1 unchanged; compare grapheme boundary and preserve remainder. | 796 / 329 |
| `split/2` | 861 | direction=leading | Recursive empty pattern returns singleton input before direction checks; preserve split_1 matching and actual native resource/error order. | 794 / 331 |
| `split/3` | 886 | explicit arguments | Recursive empty pattern returns singleton input before direction checks; preserve split_1 matching and actual native resource/error order. | 245 / 439 |
| `replace/3` | 907 | direction=leading | Use split result then lists:join with arbitrary replacement term; preserve nested output and replace call frames. | 196 / 273 |
| `replace/4` | 937 | explicit arguments | Use split result then lists:join with arbitrary replacement term; preserve nested output and replace call frames. | 258 / 563 |
| `find/2` | 999 | direction=leading | Only literal [] or <<>> bypass direction and conversion; normalized-empty nested patterns reach native compile error; leading/trailing boundaries and native resources. | 768 / 357 |
| `find/3` | 1027, 1028, 1029, 1031 | explicit arguments | Only literal [] or <<>> bypass direction and conversion; normalized-empty nested patterns reach native compile error; leading/trailing boundaries and native resources. | 114 / 498 |
| `jaro_similarity/2` | 1061 | explicit arguments | Left grapheme list before right index-map traversal; strict open matching window, earliest unused candidate, sorted right matches and identical floating arithmetic order. | 3943 / 166 |
| `next_codepoint/1` | 1139 | explicit arguments | Direct pinned unicode_util:cp result, including error tuple and improper chardata continuation; no error rewriting. | 736 / 42 |
| `next_grapheme/1` | 1120 | explicit arguments | Direct pinned unicode_util:gc result and CRLF/combining/ZWJ cluster representation; no error rewriting. | 728 / 50 |
| `to_float/1` | 805 | explicit arguments | Take numeric prefix, one Unicode list conversion, actual native list parser, exact original remainder, catch Erlang errors as badarg. | 1325 / 0 |
| `to_integer/1` | 764 | explicit arguments | Take numeric prefix, one Unicode list conversion, actual string list parser, exact original remainder, catch Erlang errors as badarg; 4096 digits avoid repeated traversal. | 355 / 0 |
| `len/1` | 2425 | explicit arguments | Native list length semantics and original string caller frame; binary rejected, arbitrary list elements permitted. | 19 / 44 |
| `concat/2` | 2454 | explicit arguments | Actual list append primitive: arbitrary right tail, left traversal and improper-left error order. | 91 / 124 |
| `chr/2` | 2472 | explicit arguments | Integer target; one-based first exact match can return before malformed tail; zero for absent match. | 37 / 178 |
| `rchr/2` | 2490 | explicit arguments | Integer target; one-based last exact match requires traversal to end, including improper-tail helper failure. | 35 / 180 |
| `str/2` | 2516 | explicit arguments | List pattern and exact element equality; first one-based match, original empty-pattern behavior and lazy improper suffix. | 34 / 182 |
| `rstr/2` | 2538 | explicit arguments | List pattern and exact element equality; last match requires full source traversal and correct prefix helper failures. | 34 / 182 |
| `span/2` | 2567 | explicit arguments | Actual lists:member exact equality; stop on first nonmember without reading suffix; preserve malformed separator errors. | 35 / 180 |
| `cspan/2` | 2588 | explicit arguments | Actual lists:member exact equality; stop on first member without reading suffix; preserve malformed separator errors. | 38 / 178 |
| `substr/2` | 2608, 2610 | explicit arguments | One-based drop then keep, source integer guards and zero-length precedence; reached tail must match original list clause. | 23 / 193 |
| `substr/3` | 2626 | explicit arguments | One-based drop then keep, source integer guards and zero-length precedence; reached tail must match original list clause. | 33 / 415 |
| `tokens/2` | 2654 | explicit arguments | Distinct zero/single/multiple separator branches, exact term membership, suppress empty tokens and preserve reverse helper errors. | 35 / 181 |
| `chars/2` | 2704 | tail=[] | Positive count recurses with arbitrary C, but terminating zero clause requires integer C; arbitrary tail retained and reached error arguments preserved. | 34 / 180 |
| `chars/3` | 2720, 2722 | explicit arguments | Positive count recurses with arbitrary C, but terminating zero clause requires integer C; arbitrary tail retained and reached error arguments preserved. | 137 / 330 |
| `copies/2` | 2741 | explicit arguments | List input and nonnegative integer count guards; zero bypasses input traversal, positive count uses actual append semantics. | 31 / 183 |
| `words/1` | 2757 | separator=space | Default space; trim/count branch order and integer separator guard; obsolete flat-list behavior, not Unicode segmentation. | 19 / 44 |
| `words/2` | 2770 | explicit arguments | Default space; trim/count branch order and integer separator guard; obsolete flat-list behavior, not Unicode segmentation. | 36 / 180 |
| `strip/1` | 2824 | direction=both, separator=space | left/right/both direction vocabulary, default space, exact integer separator, left-before-right and recursive error frames. | 19 / 44 |
| `strip/2` | 2833, 2834, 2835 | separator=space | left/right/both direction vocabulary, default space, exact integer separator, left-before-right and recursive error frames. | 24 / 191 |
| `strip/3` | 2855, 2856, 2857 | explicit arguments | left/right/both direction vocabulary, default space, exact integer separator, left-before-right and recursive error frames. | 38 / 430 |
| `sub_word/2` | 2786 | separator=space | Count-before-select behavior, positive index and integer separator, source trimming and absent-word result. | 30 / 185 |
| `sub_word/3` | 2802 | explicit arguments | Count-before-select behavior, positive index and integer separator, source trimming and absent-word result. | 50 / 418 |
| `left/2` | 2884 | character=space | Integer length and pad character guards; actual native length before truncation/padding, negative length reaches original substr/chars failure. | 31 / 184 |
| `left/3` | 2901 | explicit arguments | Integer length and pad character guards; actual native length before truncation/padding, negative length reaches original substr/chars failure. | 54 / 414 |
| `right/2` | 2920 | character=space | Integer length and pad character guards; native length then one-based suffix/padding, exact original returned list. | 31 / 184 |
| `right/3` | 2936 | explicit arguments | Integer length and pad character guards; native length then one-based suffix/padding, exact original returned list. | 54 / 414 |
| `sub_string/2` | 2991 | alias substr/2 | Alias substr using inclusive Stop-Start+1 without premature narrowing or additional validation. | 24 / 191 |
| `sub_string/3` | 3006 | explicit arguments | Alias substr using inclusive Stop-Start+1 without premature narrowing or additional validation. | 33 / 435 |
| `centre/2` | 2955 | character=space | Source zero-length shortcut, native length otherwise; floor split for padding and truncation with odd deficit. | 33 / 183 |
| `centre/3` | 2970, 2972 | explicit arguments | Source zero-length shortcut, native length otherwise; floor split for padding and truncation with odd deficit. | 55 / 413 |
| `join/2` | 3086, 3088 | explicit arguments | Empty outer list guards separator; nonempty clause permits lazy separator validation; actual append/comprehension bad-generator errors. | 25 / 191 |
| `to_upper/1` | 3068, 3070 | explicit arguments | Obsolete scalar or list Latin1 mapping with fixed ranges; other integers retained, improper tail and noninteger payloads follow source helpers. | 30 / 33 |
| `to_lower/1` | 3047, 3049 | explicit arguments | Obsolete scalar or list Latin1 mapping with fixed ranges; other integers retained, improper tail and noninteger payloads follow source helpers. | 30 / 33 |
| `list_to_float/1` | 181 | explicit arguments | Owning native bif.c grammar: sign, mandatory integer/radix/fraction digits, incomplete exponent rollback, untouched tail, finite IEEE conversion; platform dependency remains explicit. | 562 / 0 |
| `list_to_integer/1` | 191 | explicit arguments | ASCII sign/digit prefix recurrence with BigInteger, distinguish not_a_list/no_integer, preserve untouched proper or improper remainder. | 78 / 0 |

The source catalog records exact handler SHA256 identities and original heads/guards.
Native `list_to_float/1` is reviewed against the C implementation, not its Erlang NIF stub.
All error observations include their full source stack; live compiled resources require actual
native queries and bijective binding. Arbitrary input references remain exact input values.
