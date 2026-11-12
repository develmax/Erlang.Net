using Erlang.Compiler;
using Erlang;

internal static class OtpWindowsReleaseProfiles
{
    private const string BuildRoot = "/daily_build/otp_src/lib/";
    private const string BootstrapKernelIncludes = "/daily_build/otp_src/bootstrap/lib/kernel/include/";
    private const string BootstrapStdlibIncludes = "/daily_build/otp_src/bootstrap/lib/stdlib/include/";
    private const string LdapBuildDirectory = BuildRoot + "eldap/src";
    private const string BootstrapCompiler = "10.0.4";
    private static readonly string[] DocumentationWarnings =
    [
        "warn_missing_doc_function",
        "warn_missing_doc_callback",
        "warn_missing_spec_documented"
    ];

    internal static BeamReleaseBuildProfile? FindForSource(string application, string relativeSource)
    {
        string module = Path.GetFileNameWithoutExtension(relativeSource);
        string? directory = relativeSource.Split(Path.DirectorySeparatorChar) switch
        {
            ["lib", var sourceApplication, "src", var subdirectory, _] when sourceApplication == application => subdirectory,
            ["lib", var sourceApplication, "src", _] when sourceApplication == application => "src",
            ["lib", "wx-2.7", "doc", "examples", ("demo" or "sudoku") and var example, _] => example,
            ["lib", "snmp-5.20.5", "doc", "examples", ("ex1" or "ex2") and var example, _] => example,
            ["lib", "megaco-4.9.2", "doc", "examples", "meas", _] => "meas",
            ["lib", "common_test-1.31.2", "proper_ext", _] => "proper_ext",
            _ => null
        };
        var profile = Find(application, module, isGeneratedWxSource: application == "wx-2.7" && directory == "gen")
            ?? (application, directory) switch
            {
                ("inets-9.8", "inets_app" or "http_lib" or "http_client" or "http_server") => InetsLibrary(module, directory),
                ("snmp-5.20.5", "agent" or "manager" or "misc" or "compiler" or "app") => SnmpLibrary(module, directory),
                ("megaco-4.9.2", "app" or "binary" or "engine" or "flex" or "tcp" or "text" or "udp")
                    => MegacoProfile(directory, module, useBuildDefinitions: true),
                ("diameter-2.7.2", "base" or "transport" or "gen" or "compiler" or "info")
                    => Diameter(directory, module) with { AllowIncompleteAbstractCode = true },
                ("wx-2.7", "demo" or "sudoku") => WxExample(directory, module),
                ("wx-2.7", "src" or "gen") => WxDriver(module, directory == "gen"),
                ("snmp-5.20.5", "ex1" or "ex2") => SnmpExample(directory, module),
                ("megaco-4.9.2", "meas") => CreateAt(BuildRoot + "megaco/examples/meas", module, ["../include"],
                    [.. DocumentationWarnings, "nowarn_missing_spec_documented", "nowarn_deprecated_catch"])
                    with { AllowIncompleteAbstractCode = true },
                ("common_test-1.31.2", "proper_ext") => CreateAt(BuildRoot + "common_test/proper_ext", module,
                    ["../include"], DocumentationWarnings) with { AllowIncompleteAbstractCode = true },
                ("ssl-11.7.7", "src") => WithBuildDefinitions(SecureTransportLibrary("ssl", module),
                    new Dictionary<string, Term> { ["VSN"] = Term.String("11.7.7") }),
                ("ssh-6.0.6", "src") => SecureTransportLibrary("ssh", module),
                ("common_test-1.31.2", "src") => Create("common_test", module,
                    ["../include", BuildRoot + "snmp/include", "../../xmerl/inc", BuildRoot + "kernel/include"],
                    [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"]) with
                    {
                        AllowIncompleteAbstractCode = true,
                        RecordedIncludeFiles = new Dictionary<string, string>
                        {
                            ["ct.hrl"] = BuildRoot + "common_test/src/../include/ct.hrl",
                            ["eep48.hrl"] = BootstrapKernelIncludes + "eep48.hrl",
                            ["file.hrl"] = BootstrapKernelIncludes + "file.hrl",
                            ["logger.hrl"] = BootstrapKernelIncludes + "logger.hrl",
                            ["ct_event.hrl"] = module == "cte_track"
                                ? "/daily_build/otp_src/bootstrap/lib/common_test/include/ct_event.hrl"
                                : BuildRoot + "common_test/src/../include/ct_event.hrl"
                        }
                    },
                _ => null
            };
        return profile is not null && SupplementalIncludeFiles(application, module) is { } includes
            ? profile with { RecordedIncludeFiles = includes }
            : profile;
    }

    private static IReadOnlyDictionary<string, string>? SupplementalIncludeFiles(string application, string module) =>
        (application, module) switch
        {
            ("kernel-11.0.4", "code_server") => new Dictionary<string, string>
            {
                ["file.hrl"] = BootstrapKernelIncludes + "file.hrl",
                ["ms_transform.hrl"] = BootstrapStdlibIncludes + "ms_transform.hrl"
            },
            ("kernel-11.0.4", "global") or ("dialyzer-6.0.3", "dialyzer_plt") or ("stdlib-8.1", "qlc_pt") => new Dictionary<string, string>
            {
                ["ms_transform.hrl"] = BootstrapStdlibIncludes + "ms_transform.hrl"
            },
            ("stdlib-8.1", "filelib") => new Dictionary<string, string>
            {
                ["file.hrl"] = BootstrapKernelIncludes + "file.hrl"
            },
            ("observer-2.19", "crashdump_viewer") => new Dictionary<string, string>
            {
                ["file.hrl"] = BuildRoot + "kernel/include/file.hrl",
                ["ms_transform.hrl"] = BuildRoot + "stdlib/include/ms_transform.hrl"
            },
            ("parsetools-2.8", "yecc") => new Dictionary<string, string>
            {
                ["erl_compile.hrl"] = BuildRoot + "stdlib/include/erl_compile.hrl",
                ["ms_transform.hrl"] = BuildRoot + "stdlib/include/ms_transform.hrl"
            },
            _ => null
        };

    internal static BeamReleaseBuildProfile? Find(
        string application,
        string module,
        bool isGeneratedWxSource = false
    ) => (application, module) switch
    {
        ("eunit-2.11", _) => Eunit(module),
        ("asn1-5.5.2", "asn1ct_table") => new(
            BootstrapCompiler,
            BuildRoot + "asn1/src/asn1ct_table.erl",
            BuildRoot + "asn1/src",
            [BuildRoot + "stdlib"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ),
        ("compiler-10.0.6", "beam_asm") => WithBuildDefinitions(new(
            BootstrapCompiler, BuildRoot + "compiler/src/beam_asm.erl", BuildRoot + "compiler/src",
            [BuildRoot + "compiler/src/../../stdlib/include", BuildRoot + "compiler/src/."],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op", "inline", "warn_unused_import", "warn_missing_spec"]
        ), new Dictionary<string, Term> { ["COMPILER_VSN"] = Term.String("10.0.6") }),
        ("dialyzer-6.0.3", _) => Dialyzer(module),
        ("compiler-10.0.6", "erl_bifs" or "beam_opcodes") => new(
            BootstrapCompiler,
            BuildRoot + "compiler/src/" + module + ".erl",
            BuildRoot + "compiler/src",
            [BuildRoot + "compiler/src/../../stdlib/include", BuildRoot + "compiler/src/."],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op", "inline", "warn_unused_import", "warn_missing_spec"]
        ),
        ("kernel-11.0.4", "erl_reply" or "erl_signal_handler" or "prim_tty_sighandler" or "logger_handler") => new(
            BootstrapCompiler,
            BuildRoot + "kernel/src/" + module + ".erl",
            BuildRoot + "kernel/src",
            [BuildRoot + "kernel/src/../include"],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op"]
        ),
        ("ssh-6.0.6", "ssh_no_io" or "ssh_daemon_channel" or "ssh_server_channel" or "ssh_sftpd_file_api"
            or "ssh_channel" or "ssh_server_key_api" or "ssh_client_key_api") => new(
            BootstrapCompiler,
            BuildRoot + "ssh/src/" + module + ".erl",
            BuildRoot + "ssh/src",
            [BuildRoot + "kernel/src"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_callback", "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            RecordedIncludeFiles: new Dictionary<string, string>
            {
                ["public_key.hrl"] = "/daily_build/otp_src/bootstrap/lib/public_key/include/public_key.hrl"
            }
        ),
        ("observer-2.19", "cdv_detail_wx" or "cdv_html_wx") => ObserverDetail(module),
        ("observer-2.19", "cdv_persistent_cb" or "cdv_atom_cb" or "cdv_timer_cb" or "cdv_fun_cb" or "cdv_dist_cb" or "cdv_port_cb" or "cdv_ets_cb" or "cdv_mod_cb" or "cdv_sched_cb" or "cdv_proc_cb" or "cdv_int_tab_cb" or "cdv_mem_cb") => Create(
            "observer", module, ["../include", "../../et/include", "../../../libraries/et/include", BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ) with
        {
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["wx.hrl"] = BuildRoot + "wx/include/wx.hrl"
            }
        },
        ("observer-2.19", "cdv_gen_cb" or "cdv_bin_cb" or "cdv_term_cb") => Create(
            "observer", module, ["../include", "../../et/include", "../../../libraries/et/include", BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ),
        ("observer-2.19", _) => Create(
            "observer", module, ["../include", "../../et/include", "../../../libraries/et/include", BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ) with { AllowIncompleteAbstractCode = true },
        ("debugger-7.0", _) => Create(
            "debugger", module, [BuildRoot.TrimEnd('/')], [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ) with
        {
            AllowIncompleteAbstractCode = true,
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["file.hrl"] = BuildRoot + "kernel/include/file.hrl",
                ["wx.hrl"] = BuildRoot + "wx/include/wx.hrl"
            }
        },
        ("megaco-4.9.2", "megaco_config_misc" or "megaco_encoder" or "megaco_transport" or "megaco_edist_compress") => MegacoProfile("engine", module),
        ("megaco-4.9.2", "megaco_ber_encoder" or "megaco_per_encoder" or "megaco_binary_encoder") => MegacoProfile("binary", module) with
        {
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["megaco_message_internal.hrl"] = BuildRoot + "megaco/src/engine/megaco_message_internal.hrl",
                ["megaco/src/engine/megaco_message_internal.hrl"] = BuildRoot + "megaco/src/engine/megaco_message_internal.hrl"
            }
        },
        ("asn1-5.5.2", _) => Asn1Library(module),
        ("erts-17.1", _) => Preloaded(module),
        ("sasl-4.4", "target_system") => SaslExample(module),
        ("eldap-1.3.1", "ELDAPv3") => Ldap(),
        ("eldap-1.3.1", "eldap") => Create("eldap", module, ["../include", "../ebin"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]) with { AllowIncompleteAbstractCode = true },
        ("crypto-5.10", _) => WithBuildDefinitions(
            Create("crypto", module, ["../include"], DocumentationWarnings) with { AllowIncompleteAbstractCode = true },
            new Dictionary<string, Term> { ["CRYPTO_VSN"] = Term.String("5.10") }
        ),
        ("wx-2.7", not "gl") when isGeneratedWxSource => WxGenerated(module),
        ("kernel-11.0.4", "pg2" or "disk_log_sup" or "inet6_tcp_dist") => Create(
            "kernel",
            module,
            ["../include"],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op"]
        ),
        ("mnesia-4.27", _) => Create(
            "mnesia",
            module,
            [],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            "mnesia_4.27"
        ) with { AllowIncompleteAbstractCode = true },
        ("sasl-4.4", _) => Create(
            "sasl",
            module,
            ["../../stdlib/include"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ) with { AllowIncompleteAbstractCode = true },
        ("tools-4.2.3", _) => Create("tools", module, [], [.. DocumentationWarnings, "nowarn_deprecated_catch"])
            with { AllowIncompleteAbstractCode = true },
        ("os_mon-2.12", _) => Create(
            "os_mon",
            module,
            ["../include"],
            DocumentationWarnings
        ) with { AllowIncompleteAbstractCode = true },
        ("runtime_tools-2.5", _) => Create(
            "runtime_tools",
            module,
            ["../include", "../../et/include", "../../../libraries/et/include"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        ) with { AllowIncompleteAbstractCode = true },
        ("ftp-1.2.6", "ftp_app") => new(
            BootstrapCompiler,
            BuildRoot + "ftp/src/ftp_app.erl",
            BuildRoot + "ftp/src",
            [BuildRoot + "kernel/src"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_catch"],
            RecordedDefinitions: new Dictionary<string, Term> { ["VSN"] = Term.String("1.2.6") }
        ),
        ("ftp-1.2.6", _) => WithBuildDefinitions(
            Create("ftp", module, [BuildRoot + "kernel/src"],
                [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_catch"])
                with { AllowIncompleteAbstractCode = true },
            new Dictionary<string, Term> { ["VSN"] = Term.String("1.2.6") }
        ),
        ("reltool-1.1", _) => Create("reltool", module, [BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"], "reltool-1.1") with
        {
            InsertedAttributeName = "app_vsn",
            WarningsBeforeInsertion = DocumentationWarnings.Length,
            AllowIncompleteAbstractCode = true
        },
        ("syntax_tools-4.1.1", _) => SyntaxTools(module),
        ("snmp-5.20.5", "snmpa_error_io") => SnmpLibrary(module, "agent"),
        ("snmp-5.20.5", "snmpm_user_default") => SnmpLibrary(module, "manager"),
        ("et-1.8", _) => Et(module),
        ("edoc-1.5", _) => Create("edoc", module, ["../include", "../../xmerl/include", BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "warn_unused_vars", "warn_unused_import", "warn_deprecated_guard",
                "no_docs", "nowarn_missing_spec_documented", "warn_deprecated_catch"]) with
        {
            EmitDocumentation = false,
            AllowIncompleteAbstractCode = true
        },
        ("parsetools-2.8", _) => Create("parsetools", module, [BuildRoot + "stdlib/include"],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"])
                with { AllowIncompleteAbstractCode = true },
        ("inets-9.8", "http_uri") => new(
            BootstrapCompiler,
            BuildRoot + "inets/src/http_lib/http_uri.erl",
            BuildRoot + "inets/src/http_lib",
            [BuildRoot + "inets/src/http_lib/../../include", BuildRoot + "inets/src/http_lib/../inets_app"],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            "inets-9.8", "app_vsn", DocumentationWarnings.Length
        ),
        ("inets-9.8", "httpd_connection_sup" or "httpd_acceptor_sup") => new(
            BootstrapCompiler,
            BuildRoot + "inets/src/http_server/" + module + ".erl",
            BuildRoot + "inets/src/http_server",
            [BuildRoot + "inets/src/http_server/../inets_app", BuildRoot + "inets/src/http_server/../http_lib"],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            "inets-9.8",
            "app_vsn",
            DocumentationWarnings.Length,
            RecordedDefinitions: new Dictionary<string, Term> { ["SERVER_SOFTWARE"] = Term.String("inets/9.8") }
        ),
        ("inets-9.8", "inets_app" or "inets_lib" or "inets_service") => new(
            BootstrapCompiler,
            BuildRoot + "inets/src/inets_app/" + module + ".erl",
            BuildRoot + "inets/src/inets_app",
            [],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            "inets-9.8",
            "app_vsn",
            DocumentationWarnings.Length
        ),
        ("snmp-5.20.5", "snmpm_user_old") => new(
            BootstrapCompiler,
            BuildRoot + "snmp/src/manager/snmpm_user_old.erl",
            BuildRoot + "snmp/src/manager",
            [BuildRoot + "snmp/src/manager/../../include", BuildRoot + "snmp/src/manager/../misc", BuildRoot + "stdlib"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch", "warn_unused_vars"],
            "snmp-5.20.5",
            "app_vsn",
            RecordedDefinitions: new Dictionary<string, Term>
            {
                ["default_verbosity"] = Term.A("silence"),
                ["version"] = Term.String("5.20.5")
            }
        ),
        ("snmp-5.20.5", "snmpa_discovery_handler_default") => new(
            BootstrapCompiler,
            BuildRoot + "snmp/src/agent/snmpa_discovery_handler_default.erl",
            BuildRoot + "snmp/src/agent",
            [BuildRoot + "snmp/src/agent/../../include", BuildRoot + "snmp/src/agent/../app", BuildRoot + "snmp/src/agent/../misc", BuildRoot + "stdlib"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_spec_documented", "nowarn_deprecated_catch"],
            "snmp-5.20.5",
            "app_vsn",
            DocumentationWarnings.Length + 1,
            new Dictionary<string, Term>
            {
                ["empty_pdu_size"] = Term.I(21),
                ["default_verbosity"] = Term.A("silence"),
                ["version"] = Term.String("5.20.5")
            }
        ),
        ("ssl-11.7.7", "inet6_tls_dist" or "tls_connection_sup" or "dtls_connection_sup" or "dtls_listener_sup"
            or "dtls_server_sup" or "dtls_sup" or "ssl_sup" or "ssl_connection_sup" or "ssl_dist_connection_sup" or "tls_sup" or "tls_server_sup" or "tls_dist_sup" or "tls_dist_server_sup"
            or "dtls_server_session_cache_sup" or "ssl_server_session_cache_sup" or "ssl_app" or "ssl_admin_sup" or "ssl_dist_admin_sup"
            or "ssl_listen_tracker_sup" or "tls_server_session_ticket_sup" or "tls_dyn_connection_sup"
            or "ssl_upgrade_server_session_cache_sup" or "ssl_dist_sup" or "ssl_client_session_cache_db" or "dtls_v1" or "dtls_socket"
            or "inet_epmd_tls_socket" or "ssl_crl_cache_api" or "ssl_session_cache_api") => new(
            BootstrapCompiler,
            BuildRoot + "ssl/src/" + module + ".erl",
            BuildRoot + "ssl/src",
            [BuildRoot + "kernel/src"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_callback", "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            RecordedDefinitions: new Dictionary<string, Term> { ["VSN"] = Term.String("11.7.7") },
            RecordedIncludeFiles: module == "inet_epmd_tls_socket"
                ? new Dictionary<string, string>
                {
                    ["dist.hrl"] = "/daily_build/otp_src/bootstrap/lib/kernel/include/dist.hrl",
                    ["dist_util.hrl"] = "/daily_build/otp_src/bootstrap/lib/kernel/include/dist_util.hrl",
                    ["logger.hrl"] = "/daily_build/otp_src/bootstrap/lib/kernel/include/logger.hrl",
                    ["net_address.hrl"] = "/daily_build/otp_src/bootstrap/lib/kernel/include/net_address.hrl",
                    ["public_key.hrl"] = "/daily_build/otp_src/bootstrap/lib/public_key/include/public_key.hrl"
                }
                : module is "dtls_server_session_cache_sup" or "ssl_server_session_cache_sup"
                or "ssl_upgrade_server_session_cache_sup" or "ssl_client_session_cache_db" or "dtls_socket"
                or "ssl_crl_cache_api" or "ssl_session_cache_api"
                ? new Dictionary<string, string>
                {
                    ["logger.hrl"] = "/daily_build/otp_src/bootstrap/lib/kernel/include/logger.hrl",
                    ["public_key.hrl"] = "/daily_build/otp_src/bootstrap/lib/public_key/include/public_key.hrl"
                }
                : null
        ),
        ("odbc-2.17.1", _) => UseBuildDefinitions(new(
            BootstrapCompiler,
            BuildRoot + "odbc/src/" + module + ".erl",
            BuildRoot + "odbc/src",
            [BuildRoot + "odbc/src/../include"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"],
            "odbc-2.17.1",
            "app_vsn",
            DocumentationWarnings.Length,
            new Dictionary<string, Term> { ["SERVER_SOFTWARE"] = Term.String("odbc/2.17.1") },
            AllowIncompleteAbstractCode: true
        )),
        ("tftp-1.3", _) => Create(
            "tftp",
            module,
            [],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_spec_documented", "warn_obsolete_bool_op", "nowarn_deprecated_catch"]
        ) with { AllowIncompleteAbstractCode = true },
        ("stdlib-8.1", "dets_sup") => Create(
            "stdlib",
            module,
            ["../include", "../../kernel/include"],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op"]
        ),
        ("ssl-11.7.7", "client_server") => new(
            BootstrapCompiler,
            BuildRoot + "ssl/examples/src/client_server.erl",
            BuildRoot + "ssl/examples/src",
            [],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_doc", "nowarn_missing_spec_documented"]
        ),        ("ssl-11.7.7", "ssl_srp_primes") => new(
            BootstrapCompiler, BuildRoot + "ssl/src/ssl_srp_primes.erl", BuildRoot + "ssl/src",
            [BuildRoot + "kernel/src"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_callback", "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            RecordedDefinitions: new Dictionary<string, Term> { ["VSN"] = Term.String("11.7.7") }),
        ("public_key-1.21.7", "AlgorithmInformation-2009" or "AttributeCertificateVersion1-2009"
            or "CMSAesRsaesOaep-2009" or "CryptographicMessageSyntax-2009"
            or "CryptographicMessageSyntaxAlgorithms-2009" or "DSS" or "ECPrivateKey"
            or "EnrollmentMessageSyntax-2009" or "KEMAlgorithmInformation-2023" or "OCSP-2024-08"
            or "OTP-PKIX" or "OTP-PKIX-Relaxed" or "PKCS-1" or "PKCS-3" or "PKCS-10" or "PKCS-FRAME"
            or "PKIX-CommonTypes-2009" or "PKIX-X400Address-2009" or "PKIX1-PSS-OAEP-Algorithms-2009"
            or "PKIX1Explicit-2009" or "PKIX1Implicit-2009" or "PKIXAlgs-2009"
            or "PKIXAttributeCertificate-2009" or "PKIXCMP-2023" or "PKIXCRMF-2009"
            or "RFC5639" or "Safecurves-pkix-18" or "SecureMimeMessageV3dot1-2009"
            or "SLH-DSA-Module-2024" or "X509-ML-DSA-2025" or "X509-ML-KEM-2025") => PublicKeyAsn1(module),
        ("xmerl-2.2", _) => Create(
            "xmerl", module, ["../include"],
            [.. DocumentationWarnings, "warn_unused_vars", "warn_obsolete_bool_op"]
        ) with
        {
            AllowIncompleteAbstractCode = true,
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["xmerl.hrl"] = BuildRoot + "xmerl/src/../include/xmerl.hrl"
            }
        },
        ("diameter-2.7.2", "diameter_etcp_sup") => Diameter("transport", module),
        ("diameter-2.7.2", "diameter_transport" or "diameter_tcp_sup") => Diameter("transport", module) with { AllowIncompleteAbstractCode = true },
        ("diameter-2.7.2", "diameter_app" or "diameter_config_sup" or "diameter_watchdog_sup" or "diameter_peer_fsm_sup" or "diameter_service_sup") => Diameter("base", module),
        ("snmp-5.20.5", "snmp_pdus_basic") => Snmp(
            module, "misc", ["../../include", "../app", "../compile", BuildRoot + "stdlib"]),
        ("snmp-5.20.5", "snmpa_net_if_filter") => SnmpLibrary(module, "agent") with
        {
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["snmp_debug.hrl"] = BuildRoot + "snmp/src/agent/../misc/snmp_debug.hrl"
            }
        },
        ("snmp-5.20.5", "snmpm_net_if_filter") => Snmp(
            module, "manager", ["../../include", "../misc", BuildRoot + "stdlib"]) with
        {
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["snmp_debug.hrl"] = BuildRoot + "snmp/src/manager/../misc/snmp_debug.hrl"
            }
        },
        ("common_test-1.31.2", "ct_suite") => Create(
            "common_test", module,
            ["../include", BuildRoot + "snmp/include", "../../xmerl/inc", BuildRoot + "kernel/include"],
            [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"]
        ),
        ("snmp-5.20.5", "snmp_config") => UseBuildDefinitions(Snmp(module, "misc",
            ["../../include", "../app", "../compile", BuildRoot + "stdlib"])),
        ("snmp-5.20.5", "snmpa_mib" or "snmpa_symbolic_store") => UseBuildDefinitions(SnmpAgentBehaviour(module) with
        {
            RecordedIncludeFiles = null
        }),
        ("snmp-5.20.5", "snmpm_user" or "snmpm_network_interface" or "snmpm_network_interface_filter") =>
            Snmp(module, "manager", ["../../include", "../misc", BuildRoot + "stdlib"]),
        ("snmp-5.20.5", "snmpa_authentication_service" or "snmpa_discovery_handler" or "snmpa_error_report"
            or "snmpa_get_mechanism" or "snmpa_mib_data" or "snmpa_mib_storage" or "snmpa_network_interface"
            or "snmpa_network_interface_filter" or "snmpa_notification_delivery_info_receiver" or "snmpa_notification_filter"
            or "snmpa_set_mechanism") => SnmpAgentBehaviour(module),
        ("public_key-1.21.7", _) => Create("public_key", module, ["../include", "../asn1"],
            [.. DocumentationWarnings, "warn_obsolete_bool_op"], "public_key-1.21.7") with
        {
            InsertedAttributeName = "app_vsn",
            AllowIncompleteAbstractCode = true
        },
        ("stdlib-8.1", _) => CoreLibrary("stdlib", module, ["../include", "../../kernel/include"]),
        ("kernel-11.0.4", "erl_epmd") => WithBuildDefinitions(CoreLibrary("kernel", module, ["../include"]),
            new Dictionary<string, Term>
            {
                ["erlang_daemon_port"] = Term.I(4369),
                ["epmd_dist_high"] = Term.I(6),
                ["epmd_dist_low"] = Term.I(5),
                ["epmd_node_type"] = Term.I(110),
                ["epmd_port_no"] = Term.I(4369)
            }),
        ("kernel-11.0.4", _) => CoreLibrary("kernel", module, ["../include"]),
        ("compiler-10.0.6", _) => CompilerLibrary(module),
        _ => null
    };

    private static BeamReleaseBuildProfile Dialyzer(string module)
    {
        var profile = Create(
            "dialyzer",
            module,
            [],
            [.. DocumentationWarnings, "warn_export_vars", "warn_unused_import", "warn_missing_spec", "warn_obsolete_bool_op"]
        ) with { AllowIncompleteAbstractCode = true };

        return module is "dialyzer_cl_parse" or "dialyzer_cplt" or "dialyzer_iplt" or "dialyzer_plt" or "typer"
            ? WithBuildDefinitions(profile, new Dictionary<string, Term> { ["VSN"] = Term.String("v6.0.3") })
            : profile;
    }

    private static BeamReleaseBuildProfile ObserverDetail(string module)
    {
        var profile = Create(
            "observer", module, ["../include", "../../et/include", "../../../libraries/et/include", BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"]
        );
        return profile with
        {
            RecordedCompileOptions =
            [
                Term.A("debug_info"),
                .. profile.IncludeDirectories.Select(path => Term.Tuple(Term.A("i"), Term.String(path))),
                .. profile.WarningOptions.Select(Term.A)
            ],
            RecordedIncludeFiles = new Dictionary<string, string>
            {
                ["wx.hrl"] = BuildRoot + "wx/include/wx.hrl"
            }
        };
    }

    private static BeamReleaseBuildProfile Eunit(string module)
    {
        const string directory = BuildRoot + "eunit/src";
        const string include = directory + "/../include";
        return new(
            BootstrapCompiler, directory + "/" + module + ".erl", directory, [include],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op", "warn_unused_import"],
            RecordedIncludeFiles: new Dictionary<string, string>
            {
                ["eunit.hrl"] = include + "/eunit.hrl",
                ["assert.hrl"] = "../../stdlib/include/assert.hrl"
            }
        );
    }

    private static BeamReleaseBuildProfile Et(string module)
    {
        string[] leadingWarnings = ["warn_unused_vars", "warn_obsolete_bool_op"];
        string[] trailingWarnings = [.. DocumentationWarnings, "nowarn_deprecated_catch"];
        var profile = Create("et", module, ["../include", BuildRoot.TrimEnd('/')], [.. leadingWarnings, .. trailingWarnings]);
        return profile with
        {
            AllowIncompleteAbstractCode = true,
            RecordedDebugOptions =
            [
                Term.Tuple(Term.A("cwd"), Term.String(profile.WorkingDirectory)),
                .. profile.IncludeDirectories.Select(path => Term.Tuple(Term.A("i"), Term.String(path))),
                .. leadingWarnings.Select(Term.A),
                Term.A("debug_info"),
                .. trailingWarnings.Select(Term.A)
            ]
        };
    }

    private static BeamReleaseBuildProfile Diameter(string subdirectory, string module) => new(
        BootstrapCompiler,
        BuildRoot + "diameter/src/" + subdirectory + "/" + module + ".erl",
        BuildRoot + "diameter/src",
        [BuildRoot + "diameter/src/../include", BuildRoot + "diameter/src/gen"],
        [.. DocumentationWarnings, "warn_export_vars", "warn_unused_vars", "nowarn_deprecated_catch"],
        "diameter-2.7.2",
        "app_vsn",
        RecordedSourceFile: subdirectory + "/" + module + ".erl",
        RecordedIncludeFiles: new Dictionary<string, string>
        {
            ["diameter.hrl"] = BuildRoot + "diameter/include/diameter.hrl",
            ["diameter_gen.hrl"] = BuildRoot + "diameter/include/diameter_gen.hrl"
        }
    );

    private static BeamReleaseBuildProfile MegacoProfile(string subdirectory, string module, bool useBuildDefinitions = false)
    {
        const string flexBuildRoot = "c:/ldisk/jenkins/workspace/WindowsWSL_maint-29-opu/otp_src/lib/";
        bool flex = subdirectory == "flex";
        string directory = (flex ? flexBuildRoot : BuildRoot) + "megaco/src/" + subdirectory;
        string include = directory + "/../../include";
        const string version = "megaco-4.9.2";
        Term define = Term.Tuple(Term.A("d"), Term.A("megaco_parser_inline"));
        Term includeOption = Term.Tuple(Term.A("i"), Term.String(include));
        Term debug = Term.A("debug_info");
        Term transform = Term.Tuple(Term.A("parse_transform"), Term.A("sys_pre_attributes"));
        Term insert = Term.Tuple(Term.A("attribute"), Term.A("insert"), Term.A("app_vsn"), Term.String(version));
        Term[] docs = DocumentationWarnings.Select(warning => (Term)Term.A(warning)).ToArray();
        string[] trailingWarnings = subdirectory is "tcp" or "udp"
            ? ["warn_obsolete_bool_op", "nowarn_deprecated_catch", "nowarn_missing_spec_documented"]
            : ["warn_obsolete_bool_op", "nowarn_deprecated_catch"];
        Term[] trailing = trailingWarnings.Select(warning => (Term)Term.A(warning)).ToArray();
        var definitions = new Dictionary<string, Term> { ["megaco_parser_inline"] = Term.A("true") };
        var compileDefinitions = new List<Term> { define };
        if (flex)
        {
            foreach (string name in new[] { "MEGACO_REENTRANT_FLEX_SCANNER", "ENABLE_MEGACO_FLEX_SCANNER" })
            {
                Term disabled = Term.A("false");
                definitions.Add(name, disabled);
                compileDefinitions.Add(Term.Tuple(Term.A("d"), Term.A(name), disabled));
            }
        }

        return new(
            flex ? "10.0.6" : BootstrapCompiler,
            directory + "/" + module + ".erl",
            directory,
            [include],
            [.. DocumentationWarnings, .. trailingWarnings],
            version,
            "app_vsn",
            RecordedCompileOptions: [debug, .. compileDefinitions, includeOption, .. docs, transform, insert, .. trailing],
            RecordedDebugOptions: [define, Term.Tuple(Term.A("cwd"), Term.String(directory)), includeOption, debug, .. docs, insert, .. trailing],
            RecordedIncludeFiles: subdirectory is "engine" or "tcp" or "udp"
                ? new Dictionary<string, string> { ["megaco.hrl"] = BuildRoot + "megaco/include/megaco.hrl" }
                : null,
            PreprocessorDefinitions: useBuildDefinitions ? definitions : null,
            AllowIncompleteAbstractCode: useBuildDefinitions
        );
    }

    private static BeamReleaseBuildProfile SyntaxTools(string module)
    {
        var profile = Create("syntax_tools", module, ["../include"],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op", "warn_unused_import"])
            with { AllowIncompleteAbstractCode = true };
        return module == "merl_transform"
            ? profile with { PreprocessorDefinitions = new Dictionary<string, Term> { ["MERL_NO_TRANSFORM"] = Term.A("true") } }
            : profile;
    }

    private static BeamReleaseBuildProfile InetsLibrary(string module, string subdirectory)
    {
        string directory = BuildRoot + "inets/src/" + subdirectory;
        string[] includes = subdirectory switch
        {
            "http_server" => ["../inets_app", "../http_lib"],
            "http_client" => ["../../include", "../inets_app", "../http_lib"],
            "http_lib" => ["../../include", "../inets_app"],
            _ => []
        };
        var recordedIncludes = new Dictionary<string, string>
        {
            ["kernel/include/file.hrl"] = BootstrapKernelIncludes + "file.hrl",
            ["kernel/include/inet.hrl"] = BootstrapKernelIncludes + "inet.hrl"
        };
        if (subdirectory == "http_lib")
            recordedIncludes["inets_internal.hrl"] = BuildRoot + "inets/src/inets_app/inets_internal.hrl";
        if (subdirectory == "http_server")
            recordedIncludes["inets/include/httpd.hrl"] = BuildRoot + "inets/include/httpd.hrl";

        var profile = new BeamReleaseBuildProfile(BootstrapCompiler, directory + "/" + module + ".erl", directory,
            ResolveIncludes(directory, includes), [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch"],
            "inets-9.8", "app_vsn", DocumentationWarnings.Length, AllowIncompleteAbstractCode: true,
            RecordedIncludeFiles: recordedIncludes);
        return subdirectory == "http_server"
            ? WithBuildDefinitions(profile, new Dictionary<string, Term> { ["SERVER_SOFTWARE"] = Term.String("inets/9.8") })
            : profile;
    }

    private static BeamReleaseBuildProfile SnmpLibrary(string module, string subdirectory)
    {
        BeamReleaseBuildProfile profile = subdirectory switch
        {
            "agent" => SnmpAgentBehaviour(module),
            "manager" => Snmp(module, "manager", ["../../include", "../misc", BuildRoot + "stdlib"]),
            "misc" => Snmp(module, "misc", ["../../include", "../app", "../compile", BuildRoot + "stdlib"]),
            "compiler" => Snmp(module, "compile", ["../../include", BuildRoot + "stdlib"]) with
            {
                RecordedDefinitions = new Dictionary<string, Term> { ["version"] = Term.String("5.20.5") }
            },
            _ => Snmp(module, "app", ["../misc", BuildRoot + "stdlib"]) with
            {
                WarningOptions = [.. DocumentationWarnings, "warn_obsolete_bool_op", "nowarn_deprecated_catch", "warn_unused_vars"]
            }
        };
        return UseBuildDefinitions(profile with { AllowIncompleteAbstractCode = true });
    }

    private static BeamReleaseBuildProfile SnmpAgentBehaviour(string module)
    {
        const string directory = BuildRoot + "snmp/src/agent";
        return new(
            BootstrapCompiler,
            directory + "/" + module + ".erl",
            directory,
            ResolveIncludes(directory, ["../../include", "../app", "../misc", BuildRoot + "stdlib"]),
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_spec_documented", "nowarn_deprecated_catch"],
            "snmp-5.20.5",
            "app_vsn",
            DocumentationWarnings.Length + 1,
            RecordedDefinitions: new Dictionary<string, Term>
            {
                ["empty_pdu_size"] = Term.I(21),
                ["default_verbosity"] = Term.A("silence"),
                ["version"] = Term.String("5.20.5")
            },
            RecordedIncludeFiles: new Dictionary<string, string>
            {
                ["snmp_types.hrl"] = BuildRoot + "snmp/include/snmp_types.hrl"
            }
        );
    }

    private static BeamReleaseBuildProfile Snmp(string module, string subdirectory, string[] includes)
    {
        string directory = BuildRoot + "snmp/src/" + subdirectory;
        return new(
            BootstrapCompiler, directory + "/" + module + ".erl", directory, ResolveIncludes(directory, includes),
            [.. DocumentationWarnings, "nowarn_deprecated_catch", "warn_unused_vars"],
            "snmp-5.20.5", "app_vsn",
            RecordedDefinitions: new Dictionary<string, Term>
            {
                ["default_verbosity"] = Term.A("silence"),
                ["version"] = Term.String("5.20.5")
            });
    }

    private static BeamReleaseBuildProfile Ldap()
    {
        const string source = LdapBuildDirectory;
        string[] includes = [source + "/../include", source + "/../ebin", ".", source + "/../asn1"];
        Term Include(int index) => Term.Tuple(Term.A("i"), Term.String(includes[index]));
        return new(BootstrapCompiler, source + "/../ebin/ELDAPv3.erl", source, includes, DocumentationWarnings,
            RecordedCompileOptions:
            [
                Term.A("debug_info"), Term.A("warnings"), Term.A("errors"), Include(0), Include(1),
                Term.A("legacy_erlang_types"), .. DocumentationWarnings.Select(Term.A),
                Term.A("nowarn_deprecated_catch"), Include(2), Include(3)
            ],
            RecordSourceFileInLineTable: true,
            RecordedDebugOptions:
            [
                Term.A("warnings"), Term.A("errors"), Term.Tuple(Term.A("cwd"), Term.String(source)),
                Include(0), Include(1), Term.A("legacy_erlang_types"), Term.A("debug_info"),
                .. DocumentationWarnings.Select(Term.A), Term.A("nowarn_deprecated_catch"), Include(2), Include(3)
            ]);
    }

    internal static string LdapAsn1Options =>
        "[warnings,ber,errors,\n"
        + $" {{cwd,\"{LdapBuildDirectory}\"}},\n {{outdir,\"{LdapBuildDirectory}/../ebin\"}},\n"
        + $" {{i,\"{LdapBuildDirectory}/../include\"}},\n {{i,\"{LdapBuildDirectory}/../ebin\"}},\n"
        + " warnings_as_errors,warnings_as_errors,legacy_erlang_types,debug_info,\n"
        + $" {DocumentationWarnings[0]},{DocumentationWarnings[1]},\n"
        + $" {DocumentationWarnings[2]},nowarn_deprecated_catch,\n {{i,\".\"}},\n"
        + $" {{i,\"{LdapBuildDirectory}/../asn1\"}}]";

    private static BeamReleaseBuildProfile Asn1Library(string module)
    {
        string[] includes = module == "asn1ct_func"
            ? [BuildRoot + "stdlib", "../rt_templates"]
            : [BuildRoot + "stdlib"];
        var profile = Create("asn1", module, includes, [.. DocumentationWarnings, "nowarn_deprecated_catch"])
            with { AllowIncompleteAbstractCode = true };

        return module == "asn1ct"
            ? WithBuildDefinitions(profile, new Dictionary<string, Term> { ["vsn"] = Term.String("5.5.2") })
            : profile;
    }

    private static BeamReleaseBuildProfile Preloaded(string module)
    {
        const string windowsBuildRoot = "c:/ldisk/jenkins/workspace/WindowsWSL_maint-29-opu/otp_src/";
        bool windowsBuild = module == "prim_eval";
        string root = windowsBuild ? windowsBuildRoot : BuildRoot[..^"lib/".Length];
        string directory = root + "erts/preloaded/src";

        return new(
            windowsBuild ? "10.0.6" : BootstrapCompiler,
            directory + "/" + module + (windowsBuild ? ".S" : ".erl"),
            directory,
            [root + "lib/kernel/src", root + "lib/kernel/include"],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"],
            AllowIncompleteAbstractCode: true
        );
    }

    private static BeamReleaseBuildProfile WxExample(string subdirectory, string module)
    {
        string directory = BuildRoot + "wx/examples/" + subdirectory;
        bool usesRelativeWxHeader = subdirectory == "sudoku" || module is "ex_aui" or "ex_htmlWindow";

        return new(
            BootstrapCompiler,
            directory + "/" + module + ".erl",
            directory,
            [BuildRoot.TrimEnd('/')],
            [.. DocumentationWarnings, "nowarn_missing_doc", "nowarn_missing_spec_documented", "nowarn_deprecated_catch"],
            RecordedIncludeFiles: new Dictionary<string, string>
            {
                ["wx.hrl"] = usesRelativeWxHeader ? "../../include/wx.hrl" : BuildRoot + "wx/include/wx.hrl",
                ["gl.hrl"] = BuildRoot + "wx/include/gl.hrl",
                ["glu.hrl"] = BuildRoot + "wx/include/glu.hrl"
            },
            AllowIncompleteAbstractCode: true
        );
    }

    private static BeamReleaseBuildProfile SaslExample(string module)
    {
        const string directory = BuildRoot + "sasl/examples/src";

        return new(
            BootstrapCompiler,
            directory + "/" + module + ".erl",
            directory,
            [],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_spec_documented"],
            AllowIncompleteAbstractCode: true
        );
    }

    private static BeamReleaseBuildProfile WxGenerated(string module)
    {
        const string directory = BuildRoot + "wx/src";
        string sourceFile = "gen/" + module + ".erl";
        string[] includes = [directory + "/../include", BuildRoot.TrimEnd('/')];
        string[] warnings = [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_doc",
            "warn_obsolete_bool_op", "nowarn_deprecated_catch"];
        Term define = Term.Tuple(Term.A("d"), Term.A("CAN_BUILD_DRIVER"));
        Term debug = Term.A("debug_info");
        var includeOptions = includes.Select(path => Term.Tuple(Term.A("i"), Term.String(path)));

        return new(
            BootstrapCompiler,
            directory + "/" + sourceFile,
            directory,
            includes,
            warnings,
            RecordedCompileOptions: [debug, define, .. includeOptions, .. warnings.Select(Term.A)],
            RecordedDebugOptions: [
                define, Term.Tuple(Term.A("cwd"), Term.String(directory)), .. includeOptions, debug, .. warnings.Select(Term.A)
            ],
            RecordedSourceFile: sourceFile,
            RecordedIncludeFiles: new Dictionary<string, string>
            {
                ["wx.hrl"] = directory + "/../include/wx.hrl",
                ["wxe.hrl"] = "wxe.hrl",
                ["gen/wxe_funcs.hrl"] = "gen/wxe_funcs.hrl"
            }
        );
    }

    private static BeamReleaseBuildProfile WxDriver(string module, bool generated)
    {
        const string windowsRoot = "c:/ldisk/jenkins/workspace/WindowsWSL_maint-29-opu/otp_src/lib/";
        string root = generated ? windowsRoot : BuildRoot;
        string directory = root + "wx/src";
        string[] includes = [directory + "/../include", root.TrimEnd('/')];
        string[] warnings = [.. DocumentationWarnings, "warn_unused_vars", "nowarn_missing_doc",
            "warn_obsolete_bool_op", "nowarn_deprecated_catch"];
        const string driverDefinition = "CAN_BUILD_DRIVER";
        Term define = Term.Tuple(Term.A("d"), Term.A(driverDefinition));
        Term debug = Term.A("debug_info");
        Term[] includeOptions = includes.Select(path => (Term)Term.Tuple(Term.A("i"), Term.String(path))).ToArray();
        string source = (generated ? directory + "/gen" : directory) + "/" + module + ".erl";

        return new(
            generated ? "10.0.6" : BootstrapCompiler,
            source,
            directory,
            includes,
            warnings,
            RecordedCompileOptions: [debug, define, .. includeOptions, .. warnings.Select(Term.A)],
            RecordedDebugOptions: [define, Term.Tuple(Term.A("cwd"), Term.String(directory)), .. includeOptions,
                debug, .. warnings.Select(Term.A)],
            RecordedSourceFile: generated ? "gen/" + module + ".erl" : null,
            PreprocessorDefinitions: new Dictionary<string, Term> { [driverDefinition] = Term.A("true") },
            AllowIncompleteAbstractCode: true
        );
    }

    private static BeamReleaseBuildProfile SnmpExample(string subdirectory, string module) =>
        WithBuildDefinitions(CreateAt(BuildRoot + "snmp/examples/" + subdirectory, module,
            ["../include", BuildRoot + "stdlib"],
            [.. DocumentationWarnings, "nowarn_missing_doc", "nowarn_missing_spec_documented", "nowarn_deprecated_catch"],
            "snmp-5.20.5") with
        {
            InsertedAttributeName = "app_vsn",
            WarningsBeforeInsertion = DocumentationWarnings.Length,
            AllowIncompleteAbstractCode = true
        }, new Dictionary<string, Term> { ["version"] = Term.String("5.20.5") });

    private static BeamReleaseBuildProfile WithBuildDefinitions(
        BeamReleaseBuildProfile profile,
        IReadOnlyDictionary<string, Term> definitions
    ) => UseBuildDefinitions(profile with { RecordedDefinitions = definitions });

    private static BeamReleaseBuildProfile UseBuildDefinitions(BeamReleaseBuildProfile profile) =>
        profile with { PreprocessorDefinitions = profile.RecordedDefinitions };

    private static BeamReleaseBuildProfile PublicKeyAsn1(string module)
    {
        string directory = BuildRoot + "public_key/asn1";
        string source = "../src/" + module + ".erl";

        return new(
            BootstrapCompiler,
            directory + "/" + source,
            directory,
            [],
            [.. DocumentationWarnings, "nowarn_deprecated_catch"],
            RecordedSourceFile: source
        );
    }

    private static BeamReleaseBuildProfile CoreLibrary(string application, string module, string[] includes) =>
        Create(application, module, includes, [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op"])
            with { AllowIncompleteAbstractCode = true };

    private static BeamReleaseBuildProfile SecureTransportLibrary(string application, string module) =>
        Create(application, module, [BuildRoot + "kernel/src"],
            [.. DocumentationWarnings, "warn_unused_vars", "nowarn_deprecated_callback",
                "warn_obsolete_bool_op", "nowarn_deprecated_catch"])
            with
            {
                AllowIncompleteAbstractCode = true,
                RecordedIncludeFiles = new Dictionary<string, string>
                {
                    ["file.hrl"] = BootstrapKernelIncludes + "file.hrl",
                    ["logger.hrl"] = BootstrapKernelIncludes + "logger.hrl",
                    ["public_key.hrl"] = "/daily_build/otp_src/bootstrap/lib/public_key/include/public_key.hrl"
                }
            };

    private static BeamReleaseBuildProfile CompilerLibrary(string module) =>
        Create("compiler", module, ["../../stdlib/include", "."],
            [.. DocumentationWarnings, "warn_deprecated_catch", "warn_obsolete_bool_op",
                .. module == "core_parse" ? Array.Empty<string>() : new[] { "inline" },
                "warn_unused_import", "warn_missing_spec"])
            with { AllowIncompleteAbstractCode = true };

    private static BeamReleaseBuildProfile Create(
        string application,
        string module,
        string[] includes,
        string[] warnings,
        string? version = null
    ) => CreateAt(BuildRoot + application + "/src", module, includes, warnings, version);

    private static BeamReleaseBuildProfile CreateAt(
        string workingDirectory,
        string module,
        string[] includes,
        string[] warnings,
        string? version = null
    ) => new(
            BootstrapCompiler,
            workingDirectory + "/" + module + ".erl",
            workingDirectory,
            ResolveIncludes(workingDirectory, includes),
            warnings,
            version
        );

    private static string[] ResolveIncludes(string directory, IEnumerable<string> includes) =>
        includes.Select(path => path.StartsWith('/') ? path : directory + "/" + path).ToArray();
}
