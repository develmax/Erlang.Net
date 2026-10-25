namespace Erlang;

internal static class PacketHttpCatalog
{
    public static readonly string[] Headers =
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

    public static readonly string[] Methods = ["OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE"];
    public static readonly Atom[] HeaderAtoms = Headers.Select(Term.A).ToArray();
    public static readonly Atom[] MethodAtoms = Methods.Select(Term.A).ToArray();
}
