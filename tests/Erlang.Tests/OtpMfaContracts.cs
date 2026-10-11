using Erlang.Differential;

namespace Erlang.Tests;

public static class OtpMfaContracts
{
    public static async Task Run(string module, string function, int arity)
    {
        string name = (module, function, arity) switch
        {
            ("gen_server", "start", 3) => "otp-server-call-cast-info",
            ("gen_server", "start", 4) => "otp-server-named-start",
            ("gen_server", "start_link", 3) => "otp-server-init-continue",
            ("gen_server", "start_link", 4) => "otp-server-named-start-link-stop-three",
            ("gen_server", "call", 2) => "otp-server-call-cast-info",
            ("gen_server", "call", 3) => "otp-server-deferred-reply",
            ("gen_server", "cast", 2) => "otp-server-call-cast-info",
            ("gen_server", "reply", 2) => "otp-server-deferred-reply",
            ("gen_server", "stop", 1) => "otp-server-call-cast-info",
            ("gen_server", "stop", 3) => "otp-server-named-start-link-stop-three",
            ("supervisor", "start_link", 2) => "otp-supervisor-map-child",
            ("supervisor", "start_link", 3) => "otp-supervisor-named",
            ("supervisor", "which_children", 1) => "otp-supervisor-child-query-order",
            ("supervisor", "count_children", 1) => "otp-supervisor-child-query-order",
            ("supervisor", "check_childspecs", 1) => "otp-supervisor-valid-check",
            ("supervisor", "check_childspecs", 2) => "otp-supervisor-significant-check-policy",
            ("supervisor", "start_child", 2) => "otp-supervisor-dynamic-lifecycle",
            ("supervisor", "terminate_child", 2) => "otp-supervisor-dynamic-lifecycle",
            ("supervisor", "restart_child", 2) => "otp-supervisor-dynamic-restart-failure-retry",
            ("supervisor", "delete_child", 2) => "otp-supervisor-dynamic-replacement",
            ("supervisor", "which_child", 2) => "otp-supervisor-simple-which-child",
            ("supervisor", "get_childspec", 2) => "otp-supervisor-simple-get-childspec",
            ("supervisor", "stop", 1) => "otp-supervisor-simple-empty-template",
            ("supervisor", "stop", 3) => "otp-supervisor-simple-stop-three",
            _ => throw new InvalidOperationException("Missing direct OTP contract")
        };
        var fixture = CompiledModuleCases.All.Single(f => f.Name == name);
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        Term actual = await CompiledModuleExecution.Run(artifact);
        if (!actual.Equals(fixture.Expected))
            throw new InvalidOperationException($"Expected {fixture.Expected}, got {actual}");
    }
}
