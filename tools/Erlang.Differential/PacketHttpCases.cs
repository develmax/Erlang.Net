namespace Erlang.Differential;

public static class PacketHttpCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-packet-http-" + name,
            "-module(packet_http_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        foreach (bool binary in new[] { false, true })
        {
            string suffix = binary ? "-binary" : "-list";
            string type = binary ? "http_bin" : "http", headerType = binary ? "httph_bin" : "httph";

            string Text(string value) => binary ? "<<\"" + value + "\">>" : "\"" + value + "\"";

            void Start(string name, string input, string output) => Add(name + suffix, "{ok," + output + ",<<9>>}=erlang:decode_packet(" + type + ",<<\"" + input + "\\r\\n\",9>>,[])");

            void Header(string name, string input, string output) => Add(name + suffix, "{ok," + output + ",<<88>>}=erlang:decode_packet(" + headerType + ",<<\"" + input + "\\r\\n\",88>>,[])");

            string[] knownHeaders =
            [
                "Cache-Control", "Connection", "Date", "Pragma", "Transfer-Encoding", "Upgrade", "Via",
                "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "From", "Host",
                "If-Modified-Since", "If-Match", "If-None-Match", "If-Range", "If-Unmodified-Since", "Max-Forwards",
                "Proxy-Authorization", "Range", "Referer", "User-Agent", "Age", "Location", "Proxy-Authenticate",
                "Public", "Retry-After", "Server", "Vary", "Warning", "Www-Authenticate", "Allow", "Content-Base",
                "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-Md5",
                "Content-Range", "Content-Type", "Etag", "Expires", "Last-Modified", "Accept-Ranges", "Set-Cookie",
                "Set-Cookie2", "X-Forwarded-For", "Cookie", "Keep-Alive", "Proxy-Connection"
            ];
            for (int index = 0; index < knownHeaders.Length; index++)
            {
                string spelling = knownHeaders[index], original = spelling.ToUpperInvariant();
                Header("known-header-" + (index + 1), original + ": v", "{http_header," + (index + 1) + ",'" + spelling + "'," + Text(original) + "," + Text("v") + "}");
            }
            foreach (string method in new[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE" })
                Start("known-method-" + method, method + " / HTTP/1.1", "{http_request,'" + method + "',{abs_path," + Text("/") + "},{1,1}}");

            Start("request", "GET /path?query HTTP/1.1", "{http_request,'GET',{abs_path," + Text("/path?query") + "},{1,1}}");
            Start("legacy", "GET /", "{http_request,'GET',{abs_path," + Text("/") + "},{0,9}}");
            Start("legacy-tab", "GET\\t/\\t", "{http_request,'GET',{abs_path," + Text("/") + "},{0,9}}");
            Start("star", "OPTIONS * HTTP/2.0", "{http_request,'OPTIONS','*',{2,0}}");
            Start("method-unknown", "PATCH / HTTP/1.1", "{http_request," + Text("PATCH") + ",{abs_path," + Text("/") + "},{1,1}}");
            Start("method-case", "get / HTTP/1.1", "{http_request," + Text("get") + ",{abs_path," + Text("/") + "},{1,1}}");
            Start("version-trailing", "GET / HTTP/1.23ignored", "{http_request,'GET',{abs_path," + Text("/") + "},{1,23}}");
            Start("response", "HTTP/1.1 200 All good", "{http_response,{1,1},200," + Text("All good") + "}");
            Start("response-empty", "HTTP/1.1 200", "{http_response,{1,1},200," + Text("") + "}");
            Start("response-no-status", "HTTP/1.1 ", "{http_response,{1,1},0," + Text("") + "}");
            Start("response-status-missing-error", "HTTP/1.1  reason", "{http_error," + Text("HTTP/1.1  reason\\r\\n") + "}");
            foreach (string invalid in new[] { "BAD", "GET", "GET ", "GET / HTTP/1.x", "GET / http/1.1", "HTTP/1. 200", "HTTP/1.1x200", "GET(/ HTTP/1.1" })
                Start("error-" + cases.Count, invalid, "{http_error," + Text(invalid + "\\r\\n") + "}");
            Start(
                "uri-http",
                "GET http://host:80/p?q HTTP/1.1",
                "{http_request,'GET',{absoluteURI,http," + Text("host") + ",80," + Text("/p?q") + "},{1,1}}"
            );
            Start(
                "uri-case-https",
                "GET HTTPS://host HTTP/1.1",
                "{http_request,'GET',{absoluteURI,https," + Text("host") + ",undefined," + Text("/") + "},{1,1}}"
            );
            Start(
                "uri-port-zero",
                "GET http://host:0 HTTP/1.1",
                "{http_request,'GET',{absoluteURI,http," + Text("host") + ",undefined," + Text("/") + "},{1,1}}"
            );
            Start(
                "uri-port-invalid",
                "GET http://host:12x/path HTTP/1.1",
                "{http_request,'GET',{absoluteURI,http," + Text("host") + ",undefined," + Text("/path") + "},{1,1}}"
            );
            Start(
                "uri-ipv6",
                "GET https://[::1]:4000/a HTTP/1.1",
                "{http_request,'GET',{absoluteURI,https," + Text("[::1]") + ",4000," + Text("/a") + "},{1,1}}"
            );
            Start(
                "uri-ipv6-no-port",
                "GET http://[::1]/ HTTP/1.1",
                "{http_request,'GET',{absoluteURI,http," + Text("[::1]") + ",undefined," + Text("/") + "},{1,1}}"
            );
            Start("uri-scheme", "GET ftp:thing HTTP/1.1", "{http_request,'GET',{scheme," + Text("ftp") + "," + Text("thing") + "},{1,1}}");
            Start("uri-string", "GET abc HTTP/1.1", "{http_request,'GET'," + Text("abc") + ",{1,1}}");
            Start("uri-single", "GET x HTTP/1.1", "{http_request,'GET',{abs_path," + Text("x") + "},{1,1}}");
            Header("header-host", "hOsT:  example ", "{http_header,14,'Host'," + Text("hOsT") + "," + Text("example ") + "}");
            Header("header-custom", "x-TEST: value", "{http_header,0," + Text("X-Test") + "," + Text("x-TEST") + "," + Text("value") + "}");
            Header("header-empty-name", ": value", "{http_header,0," + Text("") + "," + Text("") + "," + Text("value") + "}");
            Header("header-empty-value", "Host:", "{http_header,14,'Host'," + Text("Host") + "," + Text("") + "}");
            Header(
                "header-folded",
                "Host: one\\r\\n two\\r\\n\\tthree",
                "{http_header,14,'Host'," + Text("Host") + "," + Text("one\\r\\n two\\r\\n\\tthree") + "}"
            );
            string longName = new('x', 51);
            Header("header-long", longName + ": value", "{http_header,0," + Text(longName) + "," + Text(longName) + "," + Text("value") + "}");
            string exactName = new('x', 50), normalizedExactName = "X" + new string('x', 49);
            Header(
                "header-exact-normalization-limit",
                exactName + ": v",
                "{http_header,0," + Text(normalizedExactName) + "," + Text(exactName) + "," + Text("v") + "}"
            );
            Header("header-leading-hyphen", "-FOO: v", "{http_header,0," + Text("-foo") + "," + Text("-FOO") + "," + Text("v") + "}");
            Header("header-double-hyphen", "x--TEST: v", "{http_header,0," + Text("X--test") + "," + Text("x--TEST") + "," + Text("v") + "}");
            Header("header-error", "Bad Name: value", "{http_error," + Text("Bad Name: value\\r\\n") + "}");
            Header("header-eoh", "", "http_eoh");
            Add("header-await-next-byte" + suffix, "{more,undefined}=erlang:decode_packet(" + headerType + ",<<\"Host: value\\r\\n\">>,[])");
            Add("header-tab-is-continuation" + suffix, "{more,undefined}=erlang:decode_packet(" + headerType + ",<<\"Host: v\\r\\n\",9>>,[])");
            Add(
                "header-limit-before-next-byte" + suffix,
                "{more,undefined}=erlang:decode_packet(" + headerType + ",<<\"Host: v\\r\\n\">>,[{packet_size,9}]),{error,invalid}=erlang:decode_packet(" + headerType + ",<<\"Host: v\\r\\n\">>,[{packet_size,8}])"
            );
            Add(
                "blank-line-bypasses-limit" + suffix,
                "{ok,http_eoh,<<>>}=erlang:decode_packet(" + headerType + ",<<13,10>>,[{packet_size,1}])"
            );
            Add(
                "empty-eoh" + suffix,
                "{ok,http_eoh,<<>>}=erlang:decode_packet(" + headerType + ",<<13,10>>,[]),{ok,http_eoh,<<>>}=erlang:decode_packet(" + headerType + ",<<10>>,[])"
            );
            Add("empty-start-error" + suffix, "{ok,{http_error," + Text("\\r\\n") + "},<<>>}=erlang:decode_packet(" + type + ",<<13,10>>,[])");
            Add(
                "limit-no-newline" + suffix,
                "{error,invalid}=erlang:decode_packet(" + type + ",<<\"GET\">>,[{packet_size,3}]),{more,undefined}=erlang:decode_packet(" + type + ",<<\"GET\">>,[{packet_size,4}])"
            );
            Add(
                "line-limit-fallback" + suffix,
                "{error,invalid}=erlang:decode_packet(" + type + ",<<\"GET\">>,[{line_length,3}]),{more,undefined}=erlang:decode_packet(" + type + ",<<\"GET\">>,[{packet_size,4},{line_length,1}])"
            );
            Add(
                "delimiter-rejected" + suffix,
                "ok=try erlang:decode_packet(" + type + ",<<>>,[{line_delimiter,10}]) of _->unexpected catch error:badarg->ok end"
            );
        }

        return cases;
    }
}
