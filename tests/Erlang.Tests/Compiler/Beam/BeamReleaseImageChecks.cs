using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamReleaseImageChecks
{
    internal const string TestName = "compiler/beam/rebuild-installed-otp-release-images";
    private const string Failure = "Regenerated OTP release image differs: ";

    internal static Task Run()
    {
        (string Application, string Module)[] modules =
        [
            ("diameter-2.7.2", "diameter_app"),
            ("diameter-2.7.2", "diameter_transport"),
            ("diameter-2.7.2", "diameter_tcp_sup"),
            ("erts-17.1", "erl_tracer"),
            ("erts-17.1", "prim_buffer"),
            ("et-1.8", "et"),
            ("kernel-11.0.4", "logger_handler"),
            ("megaco-4.9.2", "megaco_encoder"),
            ("snmp-5.20.5", "snmpa_mib_data"),
            ("snmp-5.20.5", "snmpa_error_io"),
            ("snmp-5.20.5", "snmpm_user_default"),
            ("snmp-5.20.5", "snmpa_notification_delivery_info_receiver"),
            ("ssl-11.7.7", "ssl_crl_cache_api"),
            ("common_test-1.31.2", "ct_suite"),
            ("inets-9.8", "inets_service"),
            ("inets-9.8", "http_uri"),
            ("inets-9.8", "httpc_handler_sup"),
            ("megaco-4.9.2", "megaco_edist_compress"),
            ("megaco-4.9.2", "megaco_transport"),
            ("snmp-5.20.5", "snmpa_authentication_service"),
            ("snmp-5.20.5", "snmpa_discovery_handler"),
            ("snmp-5.20.5", "snmpa_error_report"),
            ("snmp-5.20.5", "snmpa_get_mechanism"),
            ("snmp-5.20.5", "snmpa_mib_storage"),
            ("snmp-5.20.5", "snmpa_network_interface"),
            ("snmp-5.20.5", "snmpa_network_interface_filter"),
            ("snmp-5.20.5", "snmpa_notification_filter"),
            ("snmp-5.20.5", "snmpa_set_mechanism"),
            ("snmp-5.20.5", "snmpm_network_interface"),
            ("snmp-5.20.5", "snmpm_network_interface_filter"),
            ("snmp-5.20.5", "snmpm_user"),
            ("ssh-6.0.6", "ssh_daemon_channel"),
            ("ssh-6.0.6", "ssh_channel"),
            ("ssh-6.0.6", "ssh_server_key_api"),
            ("ssh-6.0.6", "ssh_client_key_api"),
            ("ssh-6.0.6", "ssh_server_channel"),
            ("ssh-6.0.6", "ssh_sftpd_file_api"),
            ("ssh-6.0.6", "ssh_lsocket_sup"),
            ("ssh-6.0.6", "ssh_tcpip_forward_acceptor_sup"),
            ("ssl-11.7.7", "ssl_session_cache_api"),
            ("tftp-1.3", "tftp"),
            ("wx-2.7", "wxClipboardTextEvent"),
            ("wx-2.7", "wx_misc"),
            ("wx-2.7", "wxDisplay"),
            ("wx-2.7", "wxDisplayChangedEvent"),
            ("wx-2.7", "wxInitDialogEvent"),
            ("wx-2.7", "wxGraphicsObject"),
            ("wx-2.7", "wxPaintDC"),
            ("wx-2.7", "wxClientDC"),
            ("wx-2.7", "wxWindowDC"),
            ("wx-2.7", "wxBoxSizer"),
            ("wx-2.7", "wxGraphicsMatrix"),
            ("wx-2.7", "wxIdleEvent"),
            ("wx-2.7", "wxPickerBase"),
            ("wx-2.7", "wxJoystickEvent"),
            ("wx-2.7", "wxMouseEvent"),
            ("wx-2.7", "wxSizerFlags"),
            ("wx-2.7", "wxAuiPaneInfo"),
            ("wx-2.7", "wxCloseEvent"),
            ("wx-2.7", "wxEvent"),
            ("wx-2.7", "wxClipboard"),
            ("wx-2.7", "wxCaret"),
            ("wx-2.7", "wxAuiManagerEvent"),
            ("wx-2.7", "wxFindReplaceData"),
            ("wx-2.7", "wxGridCellFloatEditor"),
            ("wx-2.7", "wxGridCellFloatRenderer"),
            ("wx-2.7", "wxGridCellNumberEditor"),
            ("wx-2.7", "wxGridCellTextEditor"),
            ("wx-2.7", "wxTextDataObject"),
            ("wx-2.7", "wxToolTip"),
            ("wx-2.7", "wxPalette"),
            ("wx-2.7", "wxControl"),
            ("wx-2.7", "wxCommandEvent"),
            ("wx-2.7", "wxFileDataObject"),
            ("wx-2.7", "wxPrintData"),
            ("wx-2.7", "wxTreeEvent"),
            ("wx-2.7", "wxUpdateUIEvent"),
            ("wx-2.7", "wxDCOverlay"),
            ("wx-2.7", "wxGridCellRenderer"),
            ("wx-2.7", "wxMirrorDC"),
            ("wx-2.7", "wxNotifyEvent"),
            ("wx-2.7", "wxAuiNotebookEvent"),
            ("wx-2.7", "wxBookCtrlEvent"),
            ("wx-2.7", "wxContextMenuEvent"),
            ("wx-2.7", "wxFontDialog"),
            ("wx-2.7", "wxHelpEvent"),
            ("wx-2.7", "wxNavigationKeyEvent"),
            ("wx-2.7", "wxOverlay"),
            ("wx-2.7", "wxPaletteChangedEvent"),
            ("wx-2.7", "wxQueryNewPaletteEvent"),
            ("wx-2.7", "wxSetCursorEvent"),
            ("wx-2.7", "wxShowEvent"),
            ("wx-2.7", "wxSpinEvent"),
            ("wx-2.7", "wxSplitterEvent"),
            ("wx-2.7", "wxStdDialogButtonSizer"),
            ("wx-2.7", "wxAuiSimpleTabArt"),
            ("wx-2.7", "wxGridCellBoolRenderer"),
            ("wx-2.7", "wxGridCellNumberRenderer"),
            ("wx-2.7", "wxGridCellStringRenderer"),
            ("wx-2.7", "wxLogNull"),
            ("wx-2.7", "wxPostScriptDC"),
            ("wx-2.7", "wxScreenDC"),
            ("wx-2.7", "wxActivateEvent"),
            ("wx-2.7", "wxCalendarEvent"),
            ("wx-2.7", "wxChildFocusEvent"),
            ("wx-2.7", "wxColourPickerEvent"),
            ("wx-2.7", "wxDateEvent"),
            ("wx-2.7", "wxDropFilesEvent"),
            ("wx-2.7", "wxEraseEvent"),
            ("wx-2.7", "wxFileDirPickerEvent"),
            ("wx-2.7", "wxFocusEvent"),
            ("wx-2.7", "wxFontPickerEvent"),
            ("wx-2.7", "wxGridEvent"),
            ("wx-2.7", "wxHtmlLinkEvent"),
            ("wx-2.7", "wxIconizeEvent"),
            ("wx-2.7", "wxKeyEvent"),
            ("wx-2.7", "wxListEvent"),
            ("wx-2.7", "wxMenuEvent"),
            ("wx-2.7", "wxMouseCaptureChangedEvent"),
            ("wx-2.7", "wxMoveEvent"),
            ("wx-2.7", "wxSashEvent"),
            ("wx-2.7", "wxScrollEvent"),
            ("wx-2.7", "wxScrollWinEvent"),
            ("wx-2.7", "wxSizeEvent"),
            ("wx-2.7", "wxStyledTextEvent"),
            ("wx-2.7", "wxWebViewEvent"),
            ("wx-2.7", "wxMaximizeEvent"),
            ("wx-2.7", "wxMouseCaptureLostEvent"),
            ("wx-2.7", "wxPaintEvent"),
            ("wx-2.7", "wxSysColourChangedEvent"),
            ("wx-2.7", "wxTaskBarIconEvent"),
            ("wx-2.7", "wxWindowCreateEvent"),
            ("wx-2.7", "wxWindowDestroyEvent"),
            ("asn1-5.5.2", "asn1ct_rtt"),
            ("asn1-5.5.2", "asn1ct_table"),
            ("observer-2.19", "cdv_gen_cb"),
            ("observer-2.19", "cdv_persistent_cb"),
            ("observer-2.19", "cdv_atom_cb"),
            ("observer-2.19", "cdv_timer_cb"),
            ("observer-2.19", "cdv_fun_cb"),
            ("observer-2.19", "cdv_dist_cb"),
            ("observer-2.19", "cdv_port_cb"),
            ("observer-2.19", "cdv_ets_cb"),
            ("observer-2.19", "cdv_mod_cb"),
            ("observer-2.19", "cdv_sched_cb"),
            ("observer-2.19", "cdv_proc_cb"),
            ("observer-2.19", "cdv_bin_cb"),
            ("observer-2.19", "cdv_int_tab_cb"),
            ("observer-2.19", "cdv_mem_cb"),
            ("observer-2.19", "cdv_term_cb"),
            ("observer-2.19", "cdv_detail_wx"),
            ("observer-2.19", "cdv_html_wx"),
            ("megaco-4.9.2", "megaco_config_misc"),
            ("megaco-4.9.2", "megaco_ber_encoder"),
            ("megaco-4.9.2", "megaco_binary_encoder"),
            ("megaco-4.9.2", "megaco_per_encoder"),
            ("kernel-11.0.4", "pg2"),
            ("kernel-11.0.4", "erl_reply"),
            ("kernel-11.0.4", "erl_signal_handler"),
            ("kernel-11.0.4", "prim_tty_sighandler"),
            ("sasl-4.4", "sasl_report_tty_h"),
            ("compiler-10.0.6", "erl_bifs"),
            ("compiler-10.0.6", "beam_opcodes"),
            ("mnesia-4.27", "mnesia_registry"),
            ("mnesia-4.27", "mnesia_checkpoint_sup"),
            ("mnesia-4.27", "mnesia_sp"),
            ("mnesia-4.27", "mnesia_ext_sup"),
            ("os_mon-2.12", "os_mon_mib"),
            ("runtime_tools-2.5", "erts_alloc_config"),
            ("ftp-1.2.6", "ftp_app"),
            ("inets-9.8", "inets_app"),
            ("inets-9.8", "inets_lib"),
            ("inets-9.8", "httpd_connection_sup"),
            ("inets-9.8", "httpd_acceptor_sup"),
            ("snmp-5.20.5", "snmpa_discovery_handler_default"),
            ("snmp-5.20.5", "snmp_pdus_basic"),
            ("snmp-5.20.5", "snmpm_net_if_filter"),
            ("kernel-11.0.4", "inet6_tcp_dist"),
            ("ssl-11.7.7", "inet6_tls_dist"),
            ("ssl-11.7.7", "client_server"),
            ("ssl-11.7.7", "ssl_srp_primes"),
            ("ssl-11.7.7", "ssl_app"),
            ("ssl-11.7.7", "ssl_admin_sup"),
            ("ssh-6.0.6", "ssh_no_io"),
            ("odbc-2.17.1", "odbc_app"),
            ("odbc-2.17.1", "odbc_sup"),
            ("diameter-2.7.2", "diameter_etcp_sup"),
            ("diameter-2.7.2", "diameter_config_sup"),
            ("diameter-2.7.2", "diameter_watchdog_sup"),
            ("diameter-2.7.2", "diameter_peer_fsm_sup"),
            ("diameter-2.7.2", "diameter_service_sup"),
            ("xmerl-2.2", "xmerl_text"),
            ("xmerl-2.2", "xmerl_sgml"),
            ("xmerl-2.2", "xmerl_xpath_lib"),
            ("xmerl-2.2", "xmerl_b64Bin_scan"),
            ("public_key-1.21.7", "KEMAlgorithmInformation-2023"),
            ("runtime_tools-2.5", "runtime_tools_sup"),
            ("kernel-11.0.4", "disk_log_sup"),
            ("ssl-11.7.7", "tls_connection_sup"),
            ("ssl-11.7.7", "dtls_connection_sup"),
            ("ssl-11.7.7", "dtls_listener_sup"),
            ("ssl-11.7.7", "ssl_sup"),
            ("ssl-11.7.7", "ssl_connection_sup"),
            ("ssl-11.7.7", "ssl_dist_connection_sup"),
            ("ssl-11.7.7", "ssl_dist_admin_sup"),
            ("ssl-11.7.7", "ssl_dist_sup"),
            ("ssl-11.7.7", "ssl_listen_tracker_sup"),
            ("ssl-11.7.7", "tls_server_session_ticket_sup"),
            ("ssl-11.7.7", "tls_dyn_connection_sup"),
            ("ssl-11.7.7", "tls_dist_server_sup"),
            ("ssl-11.7.7", "tls_dist_sup"),
            ("ssl-11.7.7", "tls_server_sup"),
            ("ssl-11.7.7", "tls_sup"),
            ("ssl-11.7.7", "dtls_server_sup"),
            ("ssl-11.7.7", "dtls_sup"),
            ("ssl-11.7.7", "dtls_server_session_cache_sup"),
            ("ssl-11.7.7", "ssl_server_session_cache_sup"),
            ("ssl-11.7.7", "ssl_upgrade_server_session_cache_sup"),
            ("ssl-11.7.7", "ssl_client_session_cache_db"),
            ("ssl-11.7.7", "dtls_v1"),
            ("ssl-11.7.7", "dtls_socket"),
            ("ssl-11.7.7", "inet_epmd_tls_socket"),
            ("tftp-1.3", "tftp_app"),
            ("mnesia-4.27", "mnesia_app"),
            ("runtime_tools-2.5", "runtime_tools"),
            ("stdlib-8.1", "dets_sup"),
            ("stdlib-8.1", "erl_posix_msg"),
            ("stdlib-8.1", "shell_default"),
            ("crypto-5.10", "crypto_ec_curves"),
            ("snmp-5.20.5", "snmpm_user_old"),
            ("mnesia-4.27", "mnesia_backend_type")
        ];
        var temporary = Directory.CreateTempSubdirectory("erlang-release-beam-");
        try
        {
            CompareSpecMetadata(temporary.FullName, "spec_probe");
            CompareSpecMetadata(temporary.FullName, "beam_qualified_spec_probe");
            CompareImportMetadata(temporary.FullName);
            CompareRecordGuardMetadata(temporary.FullName);
            CompareDebugFunctions(temporary.FullName, "beam_map_catch_debug_probe", 2);
            CompareDebugFunctions(temporary.FullName, "beam_comprehension_debug_probe", 5);
            CompareDebugFunctions(temporary.FullName, "beam_string_sigils", 7, "beam_string_sigils_debug.beam");
            CompareDebugFunctions(temporary.FullName, "beam_macro_sigil_probe", 1);
            CompareDebugFunctions(temporary.FullName, "beam_grouped_debug_probe", 4);
            CompareDebugFunctions(temporary.FullName, "beam_guard_grouping_probe", 1);
            CompareDebugFunctions(temporary.FullName, "beam_record_wildcard_probe", 1);
            CompareAdjacentStringDebug(temporary.FullName);
            foreach (string header in new[] { "snmp_debug.hrl", "xmerl.hrl", "httpd_internal.hrl", "crashdump_viewer.hrl",
                "observer_defs.hrl", "ssh.hrl", "ssh_connect.hrl", "ssh_transport.hrl", "ssl_internal.hrl", "ssl_handshake.hrl",
                "ssl_cipher.hrl", "ssl_record.hrl", "ssl_api.hrl", "tftp.hrl", "wxe.hrl", "wx.hrl" })
            {
                File.WriteAllBytes(Path.Combine(temporary.FullName, header), BeamModuleEmissionChecks.Read(header));
            }
            string wxGenerated = Directory.CreateDirectory(Path.Combine(temporary.FullName, "gen")).FullName;
            File.WriteAllBytes(Path.Combine(wxGenerated, "wxe_funcs.hrl"), BeamModuleEmissionChecks.Read("wxe_funcs.hrl"));
            const string wxHeader = "wx.hrl";
            string wxInclude = Directory.CreateDirectory(Path.Combine(temporary.FullName, "wx", "include")).FullName;
            File.WriteAllBytes(Path.Combine(wxInclude, wxHeader), BeamModuleEmissionChecks.Read(wxHeader));
            foreach (var (application, header) in new[] {
                ("kernel", "logger.hrl"), ("kernel", "dist.hrl"), ("kernel", "dist_util.hrl"),
                ("kernel", "net_address.hrl"), ("public_key", "public_key.hrl"),
                ("diameter", "diameter.hrl"), ("snmp", "snmp_types.hrl")
            })
            {
                string include = Directory.CreateDirectory(Path.Combine(temporary.FullName, application, "include")).FullName;
                File.WriteAllBytes(Path.Combine(include, header), BeamModuleEmissionChecks.Read(header));
            }
            const string megacoHeader = "megaco_message_internal.hrl";
            string engine = Directory.CreateDirectory(Path.Combine(temporary.FullName, "megaco", "src", "engine")).FullName;
            File.WriteAllBytes(Path.Combine(engine, megacoHeader), BeamModuleEmissionChecks.Read(megacoHeader));
            File.WriteAllBytes(Path.Combine(temporary.FullName, megacoHeader), BeamModuleEmissionChecks.Read(megacoHeader));
            foreach (var (application, module) in modules)
            {
                string input = module + ".erl";
                string path = Path.Combine(temporary.FullName, input);
                File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(input));
                byte[] expected = BeamModuleEmissionChecks.Read(module + ".beam");
                var buildProfile = module switch
                {
                    "httpc_handler_sup" => OtpWindowsReleaseProfiles.FindForSource(
                        application,
                        Path.Combine("lib", application, "src", "http_client", input)
                    ),
                    "prim_buffer" or "ssh_lsocket_sup" or "ssh_tcpip_forward_acceptor_sup" => OtpWindowsReleaseProfiles.FindForSource(
                        application,
                        Path.Combine("lib", application, "src", input)
                    ),
                    _ => OtpWindowsReleaseProfiles.Find(application, module, isGeneratedWxSource: application == "wx-2.7")
                };
                byte[] actual = CodeGeneration.CompileBeamFile(
                    path,
                    otpLibraryDirectory: temporary.FullName,
                    buildProfile: buildProfile
                );
                if (!Matches(expected, actual))
                {
                    throw new InvalidOperationException(Failure + module);
                }
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void CompareRecordGuardMetadata(string directory)
    {
        const string source = "beam_record_guard_probe.erl";
        string path = Path.Combine(directory, source);
        File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(source));
        var profile = new BeamReleaseBuildProfile("10.0.6", "/otp/" + source, "/otp", [], []);
        byte[] expected = Chunks(BeamModuleEmissionChecks.Read("beam_record_guard_probe_debug.beam")).Single(chunk => chunk.Id == "Dbgi").Data;
        byte[] actual = Chunks(CodeGeneration.CompileBeamFile(path, buildProfile: profile)).Single(chunk => chunk.Id == "Dbgi").Data;
        Term[] functions = Functions(expected);
        if (functions.Length != 3 || !functions.SequenceEqual(Functions(actual)))
        {
            throw new InvalidOperationException(Failure + source + " Dbgi functions");
        }

    }

    private static void CompareDebugFunctions(
        string directory,
        string module,
        int count,
        string? reference = null
    )
    {
        string source = module + ".erl";
        string path = Path.Combine(directory, source);
        File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(source));
        var profile = new BeamReleaseBuildProfile("10.0.6", "/otp/" + source, "/otp", [], []);
        byte[] expected = Chunks(BeamModuleEmissionChecks.Read(reference ?? module + ".beam")).Single(chunk => chunk.Id == "Dbgi").Data;
        byte[] actual = Chunks(CodeGeneration.CompileBeamFile(path, buildProfile: profile)).Single(chunk => chunk.Id == "Dbgi").Data;
        Term[] functions = Functions(expected);
        if (functions.Length != count || !functions.SequenceEqual(Functions(actual)))
        {
            throw new InvalidOperationException(Failure + source + " Dbgi functions");
        }
    }

    private static Term[] Functions(byte[] debug) =>
        ExternalTermFormat.Decode(RawEtf(debug)) is TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] }
            ? Cons.Items(forms).OfType<TupleTerm>()
                .Where(form => form.Items is [Atom { Name: "function" }, _, _, _, _]).ToArray()
            : [];

    private static void CompareImportMetadata(string directory)
    {
        const string source = "beam_import_probe.erl";
        string path = Path.Combine(directory, source);
        File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(source));
        var profile = new BeamReleaseBuildProfile("10.0.6", "/otp/" + source, "/otp", [], []);
        byte[] expected = Chunks(BeamModuleEmissionChecks.Read("beam_import_probe_debug.beam")).Single(chunk => chunk.Id == "Dbgi").Data;
        byte[] actual = Chunks(CodeGeneration.CompileBeamFile(path, buildProfile: profile)).Single(chunk => chunk.Id == "Dbgi").Data;
        Term[] imports = ImportForms(expected);
        if (imports.Length != 2 || !imports.SequenceEqual(ImportForms(actual)))
        {
            throw new InvalidOperationException(Failure + source + " Dbgi imports");
        }

        static Term[] ImportForms(byte[] debug) =>
            ExternalTermFormat.Decode(RawEtf(debug)) is TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] }
                ? Cons.Items(forms).OfType<TupleTerm>()
                    .Where(form => form.Items is [Atom { Name: "attribute" }, _, Atom { Name: "import" }, _]).ToArray()
                : [];
    }

    private static void CompareAdjacentStringDebug(string directory)
    {
        const string source = "beam_adjacent_strings_probe.erl";
        string path = Path.Combine(directory, source);
        File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(source));
        var profile = new BeamReleaseBuildProfile("10.0.6", "/otp/" + source, "/otp", [], []);
        byte[] debug = Chunks(CodeGeneration.CompileBeamFile(path, buildProfile: profile)).Single(chunk => chunk.Id == "Dbgi").Data;
        Term decoded = ExternalTermFormat.Decode(RawEtf(debug));
        if (decoded is not TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] }
            || !Cons.Items(forms).OfType<TupleTerm>().Any(form => form.Items is [
                Atom { Name: "function" }, _, Atom { Name: "text" }, _, var textClauses
            ] && Cons.Items(textClauses).SingleOrDefault() is TupleTerm { Items: [_, _, _, _, var textBody] }
                && Cons.Items(textBody).SingleOrDefault() is TupleTerm { Items: [Atom { Name: "string" }, _, var text] }
                && text.Equals(Term.String("prefix слово")))
            || !Cons.Items(forms).OfType<TupleTerm>().Any(form => form.Items is [
                Atom { Name: "function" }, _, Atom { Name: "binary_text" }, _, var binaryClauses
            ] && Cons.Items(binaryClauses).SingleOrDefault() is TupleTerm { Items: [_, _, _, _, var binaryBody] }
                && Cons.Items(binaryBody).SingleOrDefault() is TupleTerm { Items: [Atom { Name: "bin" }, _, var elements] }
                && Cons.Items(elements).SingleOrDefault() is TupleTerm
                {
                    Items: [Atom { Name: "bin_element" }, _, TupleTerm { Items: [Atom { Name: "string" }, _, var value] }, _, _]
                } && value.Equals(Term.String("onetwo"))))
        {
            throw new InvalidOperationException(Failure + source + " Dbgi");
        }
    }

    private static void CompareSpecMetadata(string directory, string module)
    {
        string source = module + ".erl";
        const string releaseDirectory = "/daily_build/otp_src/spec612";
        string path = Path.Combine(directory, source);
        File.WriteAllBytes(path, BeamModuleEmissionChecks.Read(source));
        var profile = new BeamReleaseBuildProfile("10.0.6", releaseDirectory + "/" + source, releaseDirectory, [], []);
        var expected = Chunks(BeamModuleEmissionChecks.Read(module + ".beam")).ToDictionary(chunk => chunk.Id, chunk => chunk.Data);
        var actual = Chunks(CodeGeneration.CompileBeamFile(path, buildProfile: profile)).ToDictionary(chunk => chunk.Id, chunk => chunk.Data);
        if (!Specs(expected["Dbgi"]).SequenceEqual(Specs(actual["Dbgi"]))
            || !RawEtf(expected["Docs"]).AsSpan().SequenceEqual(RawEtf(actual["Docs"])))
        {
            throw new InvalidOperationException(Failure + source);
        }
    }

    private static IEnumerable<Term> Specs(byte[] debug)
    {
        Term decoded = ExternalTermFormat.Decode(RawEtf(debug));
        if (decoded is not TupleTerm { Items: [_, _, TupleTerm { Items: [var forms, _] }] })
        {
            throw new InvalidOperationException(Failure + "Dbgi");
        }

        return Cons.Items(forms).Where(form => form is TupleTerm { Items: [Atom { Name: "attribute" }, _, Atom { Name: "spec" }, _] });
    }

    internal static bool Matches(byte[] expected, byte[] actual)
    {
        if (BeamOtpEtfCompression.HasPinnedCompressor)
        {
            return expected.AsSpan().SequenceEqual(actual);
        }

        var original = Chunks(expected);
        var generated = Chunks(actual);

        return original.Count == generated.Count && original.Zip(generated).All(pair =>
            pair.First.Id == pair.Second.Id && (pair.First.Id is "Docs" or "Dbgi"
                ? RawEtf(pair.First.Data).AsSpan().SequenceEqual(RawEtf(pair.Second.Data))
                : pair.First.Data.AsSpan().SequenceEqual(pair.Second.Data)));
    }
}
