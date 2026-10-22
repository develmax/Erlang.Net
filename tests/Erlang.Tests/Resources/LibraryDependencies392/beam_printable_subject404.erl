-module(beam_printable_subject404).
-export([worker/2, probe/1]).

worker(Parent, Case) ->
    Outcome = try probe(Case) of Value -> {returned, Value}
              catch Class:Reason:Stack -> {caught, Class, Reason, Stack} end,
    Parent ! {Case, Outcome},
    ok.

probe(range) -> io:printable_range();
probe(empty) -> io_lib:printable_list([]);
probe(ascii) -> io_lib:printable_list("hello");
probe(latin1_boundary) -> io_lib:printable_list([160, 255]);
probe(byte_control) -> io_lib:printable_list([127]);
probe(above_latin1) -> io_lib:printable_list([256]);
probe(cyrillic) -> io_lib:printable_list([1024]);
probe(emoji) -> io_lib:printable_list([16#1F600]);
probe(surrogate) -> io_lib:printable_list([16#D800]);
probe(out_of_range) -> io_lib:printable_list([16#110000]);
probe(newline) -> io_lib:printable_list([10]);
probe(tab) -> io_lib:printable_list([9]);
probe(improper) -> io_lib:printable_list([65 | bad_tail]);
probe(nested) -> io_lib:printable_list([[65]]);
probe(atom) -> io_lib:printable_list(bad_list);
probe(format_p_cyrillic) -> io_lib:format("~p", [[1024]]);
probe(format_tp_cyrillic) -> io_lib:format("~tp", [[1024]]);
probe(format_tp_emoji) -> io_lib:format("~tp", [[16#1F600]]);
probe(format_tp_latin1) -> io_lib:format("~tp", [[233]]);
probe(format_ascii) -> io_lib:format("~tp", ["text"]);
probe(format_utf8_binary) -> io_lib:format("~tp", [<<16#D0, 16#80>>]);
probe(format_ascii_binary) -> io_lib:format("~tp", [<<65, 66>>]);
probe(format_invalid_binary) -> io_lib:format("~tp", [<<16#D0>>]);
probe(format_binary_depth) -> io_lib:format("~tP", [<<16#D0, 16#80, 65>>, 2]);
probe(pretty_unicode_list) -> io_lib_pretty:print([1024]);
probe(pretty_utf8_binary) -> io_lib_pretty:print(<<16#D0, 16#80>>);
probe(error_bad_format) -> io_lib:format("~q", [1]);
probe(error_missing_argument) -> io_lib:format("~p", []);
probe(error_bad_width) -> io_lib:format("~*p", [bad_width, 1]);
probe(error_bad_character) -> io_lib:format("~c", [bad_character]).
