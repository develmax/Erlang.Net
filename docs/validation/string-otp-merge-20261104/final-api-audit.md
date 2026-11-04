# Current all71 string API source and outcome audit

Pinned OTP29.1.1; source commit ad05823719d77c8faee87348ea39513d4e2f99c5.
All71 exports and119 public source clauses are catalogued. Both explicitly selected backends independently match8522 complete observations, including6364 returned values and2158 exceptions/full stacks.
These are measured contracts on this source and assembly, without universal input/platform or physical-resource equivalence claims. Compatibility classifications and backend defaults are unchanged.

| API | Source clause lines | Handler | Values / full exceptions per backend |
| --- | --- | --- | ---: |
| `is_empty/1` | 236, 237, 238, 239 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 33 / 0 |
| `length/1` | 256, 258 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 41 / 16 |
| `to_graphemes/1` | 276 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 20 / 13 |
| `reverse/1` | 363, 365 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 24 / 15 |
| `equal/2` | 290, 292 | [StringComparisonExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringComparisonExecution.cs) | 35 / 15 |
| `equal/3` | 304, 306 | [StringComparisonExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringComparisonExecution.cs) | 45 / 23 |
| `equal/4` | 341, 343, 345 | [StringComparisonExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringComparisonExecution.cs) | 1835 / 33 |
| `slice/2` | 376 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 24 / 16 |
| `slice/3` | 403, 409, 414 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 271 / 71 |
| `pad/2` | 426 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 7 / 14 |
| `pad/3` | 435 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 7 / 22 |
| `pad/4` | 459, 462, 465 | [StringModernExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringModernExecution.cs) | 193 / 72 |
| `trim/1` | 480 | [StringTrim.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTrim.cs) | 21 / 11 |
| `trim/2` | 493 | [StringTrim.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTrim.cs) | 9 / 16 |
| `trim/3` | 522, 523, 526, 528, 531, 534 | [StringTrim.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTrim.cs) | 236 / 54 |
| `chomp/1` | 553 | [StringTrim.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTrim.cs) | 43 / 10 |
| `take/2` | 564 | [StringTake.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTake.cs) | 15 / 16 |
| `take/3` | 574 | [StringTake.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTake.cs) | 17 / 31 |
| `take/4` | 603, 611, 613, 616, 619 | [StringTake.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringTake.cs) | 401 / 69 |
| `lexemes/2` | 965, 966, 967 | [StringLexemes.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLexemes.cs) | 117 / 28 |
| `nth_lexeme/3` | 988, 989 | [StringLexemes.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLexemes.cs) | 211 / 83 |
| `uppercase/1` | 638, 642, 647, 649 | [StringCaseExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringCaseExecution.cs) | 70 / 14 |
| `lowercase/1` | 669, 673, 678, 680 | [StringCaseExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringCaseExecution.cs) | 41 / 14 |
| `titlecase/1` | 697, 702 | [StringCaseExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringCaseExecution.cs) | 90 / 10 |
| `casefold/1` | 725, 729, 734, 736 | [StringCaseExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringCaseExecution.cs) | 47 / 14 |
| `prefix/2` | 845 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 148 / 25 |
| `split/2` | 861 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 3 / 16 |
| `split/3` | 886 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 388 / 51 |
| `replace/3` | 907 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 15 / 16 |
| `replace/4` | 937 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 201 / 42 |
| `find/2` | 999 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 24 / 16 |
| `find/3` | 1027, 1028, 1029, 1031 | [StringPatternSearch.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringPatternSearch.cs) | 269 / 44 |
| `jaro_similarity/2` | 1061 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 39 / 18 |
| `next_codepoint/1` | 1139 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 33 / 8 |
| `next_grapheme/1` | 1120 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 30 / 10 |
| `to_float/1` | 805 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 87 / 0 |
| `to_integer/1` | 764 | [StringUnicode.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringUnicode.cs) | 80 / 0 |
| `len/1` | 2425 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 7 / 12 |
| `concat/2` | 2454 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 13 / 9 |
| `chr/2` | 2472 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 17 / 27 |
| `rchr/2` | 2490 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 15 / 29 |
| `str/2` | 2516 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 27 / 40 |
| `rstr/2` | 2538 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 26 / 40 |
| `span/2` | 2567 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 29 / 36 |
| `cspan/2` | 2588 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 29 / 37 |
| `substr/2` | 2608, 2610 | [StringLegacySubstringExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySubstringExecution.cs) | 5 / 20 |
| `substr/3` | 2626 | [StringLegacySubstringExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySubstringExecution.cs) | 37 / 109 |
| `tokens/2` | 2654 | [StringLegacySearchExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySearchExecution.cs) | 31 / 39 |
| `chars/2` | 2704 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 5 / 15 |
| `chars/3` | 2720, 2722 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 18 / 19 |
| `copies/2` | 2741 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 20 / 32 |
| `words/1` | 2757 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 8 / 11 |
| `words/2` | 2770 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 15 / 27 |
| `strip/1` | 2824 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 7 / 12 |
| `strip/2` | 2833, 2834, 2835 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 6 / 16 |
| `strip/3` | 2855, 2856, 2857 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 30 / 82 |
| `sub_word/2` | 2786 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 3 / 18 |
| `sub_word/3` | 2802 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 33 / 100 |
| `left/2` | 2884 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 5 / 16 |
| `left/3` | 2901 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 39 / 95 |
| `right/2` | 2920 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 5 / 16 |
| `right/3` | 2936 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 39 / 95 |
| `sub_string/2` | 2991 | [StringLegacySubstringExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySubstringExecution.cs) | 5 / 20 |
| `sub_string/3` | 3006 | [StringLegacySubstringExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacySubstringExecution.cs) | 29 / 117 |
| `centre/2` | 2955 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 8 / 16 |
| `centre/3` | 2970, 2972 | [StringLegacyWordExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyWordExecution.cs) | 42 / 92 |
| `join/2` | 3086, 3088 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 5 / 17 |
| `to_upper/1` | 3068, 3070 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 267 / 9 |
| `to_lower/1` | 3047, 3049 | [StringLegacyExecution.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacyExecution.cs) | 267 / 9 |
| `list_to_float/1` | 181 | [StringFloatNative.cs](../../../src/Runtime/Erlang.Runtime.Core/Primitives/StringFloatNative.cs) | 56 / 0 |
| `list_to_integer/1` | 191 | [StringLegacy.cs](../../../src/Otp/Managed/Erlang.Otp.Stdlib/Text/StringLegacy.cs) | 46 / 0 |

Separate depth profiles0,1,2,3,8,32 match23 outcomes each (22 exceptions/full stacks and one lazy malformed-suffix value) for each backend.
The original C#/BEAM selection, assembly digest and original/actual ETF are preserved in final-otp-comparison.tar.gz.
Current Stdlib assembly SHA256: `CDF58C84836347578A418B20DE423ACA8B550CD4EF7492D9F8600C0FB5F18CA6`.
