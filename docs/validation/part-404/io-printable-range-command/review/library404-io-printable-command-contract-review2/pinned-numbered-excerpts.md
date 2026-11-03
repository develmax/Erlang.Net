# Exact pinned printable command authority

## Complete native0arity getter
ad05823719d77c8faee87348ea39513d4e2f99c5:erts/emulator/beam/erl_unicode.c
2943: BIF_RETTYPE io_printable_range_0(BIF_ALIST_0)
2944: {
2945:     if (erts_get_printable_characters() == ERL_PRINTABLE_CHARACTERS_UNICODE) {
2946: 	BIF_RET(am_unicode);
2947:     } else {
2948: 	BIF_RET(am_latin1);
2949:     }
2950: }

## Actual default
ad05823719d77c8faee87348ea39513d4e2f99c5:erts/emulator/sys/common/erl_sys_common_misc.c
62: static int printable_character_set = ERL_PRINTABLE_CHARACTERS_LATIN1;

## Complete internal setter/getter
ad05823719d77c8faee87348ea39513d4e2f99c5:erts/emulator/sys/common/erl_sys_common_misc.c
80: void erts_set_printable_characters(int range) {
81:     /* Not an atomic */
82:     printable_character_set = range;
83: }
84: 
85: int erts_get_printable_characters(void) {
86:     return  printable_character_set;
87: }

## Complete startup valid-range mapping and rejection
ad05823719d77c8faee87348ea39513d4e2f99c5:erts/emulator/beam/erl_init.c
1371: 	case 'p':
1372: 	    if (!sys_strncmp(argv[i],"-pc",3)) {
1373: 		int printable_chars = ERL_PRINTABLE_CHARACTERS_LATIN1;
1374: 		arg = get_arg(argv[i]+3, argv[i+1], &i);
1375: 		if (!sys_strcmp(arg,"unicode")) {
1376: 		    printable_chars = ERL_PRINTABLE_CHARACTERS_UNICODE;
1377: 		} else if (sys_strcmp(arg,"latin1")) {
1378: 		    erts_fprintf(stderr, "bad range of printable "
1379: 				 "characters: %s\n", arg);
1380: 		    erts_usage();
1381: 		}
1382: 		erts_set_printable_characters(printable_chars);
1383: 	    }

## Exposed source contract/options and actual stub
ad05823719d77c8faee87348ea39513d4e2f99c5:lib/stdlib/src/io.erl
218: Returns the user-requested range of printable Unicode characters.
219: 
220: The user can request a range of characters that are to be considered printable
221: in heuristic detection of strings by the shell and by the formatting functions.
222: This is done by supplying `+pc <range>` when starting Erlang.
223: 
224: The only valid values for `<range>` are `latin1` and `unicode`. `latin1` means
225: that only code points < 256 (except control characters, and so on) are
226: considered printable. `unicode` means that all printable characters in all
227: Unicode character ranges are considered printable by the I/O functions.
228: 
229: By default, Erlang is started so that only the `latin1` range of characters
230: indicate that a list of integers is a string.
231: 
232: The simplest way to use the setting is to call `io_lib:printable_list/1`, which
233: uses the return value of this function to decide if a list is a string of
234: printable characters.
235: 
236: > #### Note {: .info }
237: >
238: > In a future release, this function may return more values and ranges. To avoid
239: > compatibility problems, it is recommended to use function
240: > `io_lib:printable_list/1`.
241: """.
242: -doc(#{since => <<"OTP R16B">>}).
243: -spec printable_range() -> 'unicode' | 'latin1'.
244: printable_range() ->
245:     erlang:nif_error(undefined).
