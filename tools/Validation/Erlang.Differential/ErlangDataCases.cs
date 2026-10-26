namespace Erlang.Differential;

public static class ErlangDataCases
{
    private static readonly IReadOnlyList<(string Function, int Arity, string Arguments, string Expected)> exports =
    [
        ("integer_to_list",1,"[-123]","\"-123\""),
        ("integer_to_list",2,"[-1023,16]","\"-3FF\""),
        ("integer_to_binary",1,"[-123]","<<\"-123\">>"),
        ("integer_to_binary",2,"[35,36]","<<\"Z\">>"),
        ("list_to_integer",1,"[\"+123\"]","123"),
        ("list_to_integer",2,"[\"-3ff\",16]","-1023"),
        ("binary_to_integer",1,"[<<\"+123\">>]","123"),
        ("binary_to_integer",2,"[<<\"Z\">>,36]","35"),
        ("list_to_binary",1,"[[1,[2],<<3>>,[]|<<4>>]]","<<1,2,3,4>>"),
        ("iolist_to_binary",1,"[<<1,2>>]","<<1,2>>"),
        ("list_to_bitstring",1,"[[<<5:3>>,<<3:2>>,7]]","<<5:3,3:2,7>>"),
        ("iolist_size",1,"[[1,[2],<<3>>|<<4>>]]","4"),
        ("binary_to_list",1,"[<<1,255>>]","[1,255]"),
        ("binary_to_list",3,"[<<1,2,3,4>>,2,3]","[2,3]"),
        ("bitstring_to_list",1,"[<<1,7:3>>]","[1,<<7:3>>]"),
        ("binary_part",2,"[<<1,2,3,4>>,{3,-2}]","<<2,3>>"),
        ("binary_part",3,"[<<1,2,3,4>>,1,2]","<<2,3>>"),
        ("split_binary",2,"[<<1,2,7:3>>,1]","{<<1>>,<<2,7:3>>}"),
        ("list_to_float",1,"[\"-1.25e2\"]","-125.0"),
        ("binary_to_float",1,"[<<\"+1,25E-2\">>]","0.0125"),
        ("crc32",1,"[\"123456789\"]","3421780262"),
        ("crc32",2,"[0,\"123456789\"]","3421780262"),
        ("adler32",1,"[\"Wikipedia\"]","300286872"),
        ("adler32",2,"[1,\"Wikipedia\"]","300286872"),
        ("crc32_combine",3,"[0,3421780262,9]","3421780262"),
        ("adler32_combine",3,"[1,300286872,9]","300286872"),
        ("md5",1,"[\"abc\"]","<<144,1,80,152,60,210,79,176,214,150,63,125,40,225,127,114>>")
    ];

    public static string? FixtureName(string function, int arity) => exports.Any(export => export.Function == function && export.Arity == arity) || function == "iolist_to_iovec" && arity == 1 ? Name(function, arity) : null;

    private static string Name(string function, int arity) => "erlang-data-export-" + function + "-" + arity;

    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-data-" + name,
        "-module(oracle_erlang_data). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call) => "try " + (call.StartsWith("erlang:", StringComparison.Ordinal) ? call : "erlang:" + call) + " of _->unexpected catch error:badarg->ok end";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = exports.Select(export => Case("export-" + export.Function + "-" + export.Arity, export.Expected + "=apply(erlang,'" + export.Function + "'," + export.Arguments + ")")).ToList();
        cases.Add(Case(
            "export-iolist_to_iovec-1",
            "L=[1,[2],<<3>>|<<4>>],V=erlang:iolist_to_iovec(L),true=lists:all(fun(X)->is_binary(X) end,V),[<<1,2,3,4>>]=V"
        ));
        cases.Add(Case(
            "iovec-chunks",
            "B= <<0:4104>>,C= <<1:4104>>,[<<1,2>>,B,<<3,4>>,C,<<5>>]=erlang:iolist_to_iovec([1,[2],B,3,<<4>>,C|<<5>>]),[B]=erlang:iolist_to_iovec(B),[B]=erlang:iolist_to_iovec([B])"
        ));
        cases.Add(Case(
            "integer-bases",
            "lists:foreach(fun(B)->lists:foreach(fun(N)->S=integer_to_list(N,B),N=list_to_integer(S,B),N=binary_to_integer(integer_to_binary(N,B),B) end,[0,1,-1,123456789012345678901234567890,-123456789012345678901234567890]) end,lists:seq(2,36))"
        ));
        cases.Add(Case(
            "integer-signs",
            "0=list_to_integer(\"-000\"),0=binary_to_integer(<<\"+000\">>),123=list_to_integer(\"000123\"),\"0\"=integer_to_list(0),\"10\"=integer_to_list(2,2)"
        ));
        foreach (string name in new[] { "list_to_integer", "binary_to_integer" })
        {
            foreach (string input in new[] { "", "+", "-", " 1", "1 ", "1.0", "--1", "1_0", "0xFF", "1e3", "1\\n" })
            {
                string argument = name == "list_to_integer" ? "\"" + input + "\"" : "<<\"" + input + "\">>";
                cases.Add(Case(name + "-invalid-" + cases.Count, Error(name + "(" + argument + ")")));
            }
        }
        foreach (string operation in new[] { "integer_to_list", "integer_to_binary", "list_to_integer", "binary_to_integer" })
        {
            string value = operation.StartsWith("integer_to", StringComparison.Ordinal) ? "12" : operation.StartsWith("binary_to", StringComparison.Ordinal) ? "<<\"12\">>" : "\"12\"";
            foreach (string radix in new[] { "1", "37", "2.0", "bad", "100000000000000000000000" })
                cases.Add(Case(operation + "-base-" + cases.Count, Error(operation + "(" + value + "," + radix + ")")));
        }
        cases.Add(Case(
            "integer-list-invalid-terms",
            Error("list_to_integer([49|bad])") + "," + Error("list_to_integer([49,256])") + "," + Error("list_to_integer([[49]])") + "," + Error("integer_to_list(1.0)") + "," + Error("binary_to_integer(<<49:7>>)") + "," + Error("list_to_integer(\"2\",2)")
        ));
        cases.Add(Case(
            "binary-list-empty",
            "[]=binary_to_list(<<>>),[]=bitstring_to_list(<<>>),<<>>=list_to_binary([]),<<>>=list_to_bitstring([]),<<>>=iolist_to_binary([]),0=iolist_size([]),[]=erlang:iolist_to_iovec([])"
        ));
        cases.Add(Case(
            "binary-slices",
            "<<>>=binary_part(<<1,2>>,2,0),<<1,2>>=binary_part(<<1,2>>,2,-2),<<>>=binary_part(<<>>,0,0),{<<>>,<<7:3>>}=split_binary(<<7:3>>,0),{<<1>>,<<7:3>>}=split_binary(<<1,7:3>>,1)"
        ));
        cases.Add(Case(
            "binary-unaligned",
            "<<_:3,B:3/binary,_:2>> = <<0:3,1,2,3,0:2>>,[1,2,3]=binary_to_list(B),<<2>>=binary_part(B,1,1),{<<1>>,<<2,3>>}=split_binary(B,1)"
        ));
        cases.Add(Case(
            "bits-concatenation",
            "<<5:3,3:2,7>>=list_to_bitstring([<<5:3>>,[<<3:2>>]|<<7>>]),[<<7:3>>]=bitstring_to_list(<<7:3>>),<<5:3,3:2>>=list_to_bitstring([<<5:3>>,<<3:2>>])"
        ));
        foreach (string operation in new[] { "list_to_binary", "iolist_to_binary", "iolist_size", "iolist_to_iovec", "list_to_bitstring" })
            foreach (string argument in new[] { "a", "1", "[1|2]", "[256]", "[-1]", "[1.0]", "[a]", "[[1|a]]" })
                cases.Add(Case(operation + "-invalid-" + cases.Count, Error(operation + "(" + argument + ")")));
        foreach (string operation in new[] { "list_to_binary", "iolist_to_binary", "iolist_size", "iolist_to_iovec" })
            cases.Add(Case(operation + "-bits", Error(operation + "([<<7:3>>])")));
        cases.Add(Case(
            "list-root-binary",
            Error("list_to_binary(<<1>>)") + "," + Error("list_to_bitstring(<<1>>)") + "," + Error("iolist_to_binary(<<7:3>>)") + "," + Error("binary_to_list(<<7:3>>)") + "," + Error("binary_to_list(<<>>,1,1)")
        ));
        foreach (string arguments in new[] { "<<1>>,0,1", "<<1>>,1,0", "<<1>>,1,2", "<<1>>,1.0,1", "<<1:1>>,1,1" })
            cases.Add(Case("binary-range-" + cases.Count, Error("binary_to_list(" + arguments + ")")));
        foreach (string arguments in new[] { "<<1>>,0,2", "<<1>>,0,-1", "<<1>>,-1,0", "<<1>>,2,0", "<<1>>,0,1.0", "<<1:1>>,0,0", "<<1>>,10000000000000000000000,0" })
            cases.Add(Case("binary-part-range-" + cases.Count, Error("binary_part(" + arguments + ")")));
        cases.Add(Case(
            "binary-tuple-range",
            Error("binary_part(<<1>>,{0})") + "," + Error("binary_part(<<1>>,a)") + "," + Error("split_binary(<<1>>,-1)") + "," + Error("split_binary(<<1>>,2)") + "," + Error("split_binary(<<1>>,1.0)")
        ));
        cases.Add(Case(
            "float-valid",
            "1.25=list_to_float(\"1,25\"),100.0=binary_to_float(<<\"1.0E+2\">>),<<1:1,0:63>> = <<(list_to_float(\"-0.0\")):64/float>>,0.0=binary_to_float(<<\"1.0e-999\">>),1.0=binary_to_float(<<49,46,48,0,120>>),1.0=list_to_float([49,46,48,0,120])"
        ));
        foreach (string operation in new[] { "list_to_float", "binary_to_float" })
            foreach (string input in new[] { "", "1", "1.", ".1", " 1.0", "1.0 ", "1.0e", "1.0e+", "1.0e999", "nan", "inf", "1.0x", "1.0e1.0" })
            {
                string argument = operation == "list_to_float" ? "\"" + input + "\"" : "<<\"" + input + "\">>";
                cases.Add(Case(operation + "-invalid-" + cases.Count, Error(operation + "(" + argument + ")")));
            }
        cases.Add(Case(
            "float-invalid-terms",
            Error("list_to_float([49,46,48|a])") + "," + Error("list_to_float([49,46,256])") + "," + Error("binary_to_float(<<1:1>>)") + "," + Error("list_to_float(1.0)")
        ));
        cases.Add(Case(
            "checksum-streaming",
            "A= <<1,2,3>>,B= <<4,5,6>>,Whole= <<A/binary,B/binary>>,lists:foreach(fun({F,C})->Expected=apply(erlang,F,[Whole]),Expected=apply(erlang,F,[apply(erlang,F,[A]),B]),Expected=apply(erlang,C,[apply(erlang,F,[A]),apply(erlang,F,[B]),byte_size(B)]) end,[{crc32,crc32_combine},{adler32,adler32_combine}])"
        ));
        cases.Add(Case(
            "checksum-iodata",
            "3421780262=erlang:crc32([49,[50,51],<<52,53>>,54|<<55,56,57>>]),300286872=erlang:adler32([<<\"Wiki\">>,\"pedia\"]),1=erlang:adler32([]),0=erlang:crc32([]),<<212,29,140,217,143,0,178,4,233,128,9,152,236,248,66,126>>=erlang:md5([])"
        ));
        cases.Add(Case("checksum-combine-empty", "7=erlang:adler32_combine(7,99,0),100=erlang:crc32_combine(7,99,0)"));
        cases.Add(Case(
            "iolist-deep",
            "D=lists:foldl(fun(_,A)->[A] end,[1,2],lists:seq(1,5000)),2=iolist_size(D),<<1,2>>=iolist_to_binary(D),[<<1,2>>]=erlang:iolist_to_iovec(D),<<1,2>>=list_to_bitstring(D)"
        ));
        cases.Add(Case(
            "integer-long-balanced",
            "S=lists:duplicate(2000,57),N=list_to_integer(S),S=integer_to_list(N),N=binary_to_integer(integer_to_binary(N)),H=lists:duplicate(1000,90),V=list_to_integer(H,36),H=integer_to_list(V,36)"
        ));
        cases.Add(Case(
            "checksum-seeds",
            "7=erlang:crc32(7,[]),7=erlang:crc32(7,<<>>),4294967295=erlang:adler32(4294967295,[]),A= <<0,255,1>>,lists:foreach(fun(F)->V=apply(erlang,F,[7,A]),V=apply(erlang,F,[7,[0,[255]|<<1>>]]) end,[crc32,adler32])"
        ));
        cases.Add(Case("binary-part-guard", "ok=guard(<<1,2>>),no=guard(<<1:1>>),no=guard(wrong)"));
        foreach (string text in new[] { "+", "-", "not", "bnot", "receive", "after", "case", "of", "end", "fun", "try", "catch", "begin", "maybe", "else", "if", "when", "(", "[", "{", "<<", "]", "}", ">>", ";", ",", "#", "->", "=", "++", "--" })
        {
            string characters = "[" + string.Join(',', text.Select(character => (int)character)) + "]";
            cases.Add(Case(
                "parser-string-token-" + cases.Count,
                characters + "=\"" + text + "\",\"" + text + "\"=\"" + text + "\"," + characters + "=binary_to_list(<<\"" + text + "\">>)"
            ));
        }
        foreach (string operation in new[] { "crc32", "adler32", "md5" })
            foreach (string argument in new[] { "1", "[1|2]", "[256]", "[<<7:3>>]", "<<7:3>>" })
                cases.Add(Case(operation + "-invalid-" + cases.Count, Error(operation + "(" + argument + ")")));
        foreach (string operation in new[] { "crc32", "adler32" })
            foreach (string initial in new[] { "-1", "4294967296", "1.0", "a" })
                cases.Add(Case(operation + "-seed-" + cases.Count, Error(operation + "(" + initial + ",[])")));
        foreach (string operation in new[] { "crc32_combine", "adler32_combine" })
            foreach (string arguments in new[] { "-1,0,0", "0,-1,0", "0,0,-1", "4294967296,0,0", "0,4294967296,0", "0,0,4294967296", "0,0,1.0" })
                cases.Add(Case(operation + "-bounds-" + cases.Count, Error(operation + "(" + arguments + ")")));

        return cases.Select(fixture => fixture.Name == "erlang-data-binary-part-guard" ? fixture with { Source = fixture.Source + " guard(B) when binary_part(B,{0,1})=:= <<1>>,binary_part(B,1,1)=:= <<2>> ->ok;guard(_)->no." } : fixture).ToArray();
    }
}
