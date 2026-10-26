namespace Erlang;

internal enum PacketDecodeType
{
    Raw,
    LengthOne,
    LengthTwo,
    LengthFour,
    SunRecord,
    Line,
    Asn1,
    Cdr,
    FastCgi,
    Tpkt,
    SslTls,
    Http,
    HttpHeaders,
    HttpBinary,
    HttpHeadersBinary
}
