namespace Erlang.Tests;

internal static class FrameOriginal481Names
{
    internal const string ContextTest = "oracle/compiled-module/frame481-context-forwarding-whole-values-and-stacks";
    internal const string RecordTest = "oracle/compiled-module/frame481-record-throw-whole-values-effects-and-stacks";
    internal const string AdmissionTest = "compiler/frame481/original-ssl-and-leex-publication";
    internal const string Prefix = "Erlang.Tests.FrameOriginal481.";
    internal const string ContextObserver = "otp_context481_observer";
    internal const string ContextImage = "otp_context481_observer.beam";
    internal const string ContextGolden = "context-values.etf";
    internal const string ContextDirectory = "context";
    internal const string RecordObserver = "otp_record481_observer";
    internal const string RecordImage = "otp_record481_observer.beam";
    internal const string RecordGolden = "record-values.etf";
    internal const string RecordDirectory = "record";
    internal const string TlsTest = "oracle/compiled-module/frame481-original-ssl-extensions-whole-values-and-stacks";
    internal const string TlsObserver = "otp_tls481_observer";
    internal const string TlsImage = "otp_tls481_observer.beam";
    internal const string TlsGolden = "tls-values.etf";
    internal const string TlsDirectory = "tls";
    internal const string SslImage = "ssl_handshake.beam";
    internal const string TlsVersionImage = "tls_v1.beam";
    internal const string ListsImage = "lists.beam";
    internal const int TlsOutcomes = 96;
    internal const string Worker = "worker";
    internal const string Manifest = "fixture.json";
    internal const string ManifestFiles = "Files";
    internal const string Managed = "managed-values.etf";
    internal const string Output = "artifacts/library481/permanent";
    internal const string Failure = "Original binary context,record throw,cleanup,full stacks or image ownership differ";
    internal const int ContextOutcomes = 10;
    internal const int RecordOutcomes = 4;
    internal static readonly string[] AdmissionImages = ["ssl_handshake.beam", "leex.beam"];
    internal static readonly string[] Imports = ["binary", "code", "crypto", "epp", "erl_anno", "erl_parse", "erl_scan", "erlang", "file", "filename", "io", "io_lib", "leex", "lists", "logger", "maps", "orddict", "ordsets", "os", "otp_context481_observer", "otp_record481_observer", "otp_tls481_observer", "proplists", "pubkey_cert_records", "public_key", "re", "sets", "ssl", "ssl_certificate", "ssl_cipher", "ssl_cipher_format", "ssl_crl", "ssl_handshake", "ssl_logger", "ssl_pkix_db", "ssl_record", "ssl_session", "ssl_srp_primes", "string", "tls_client_ticket_store", "tls_v1", "unicode"];
}
