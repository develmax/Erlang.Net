namespace Erlang;

internal static class DistributionRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.SetNode,
            DistributionRegistrationNames.ControllerAdmissionArity,
            (context, arguments) => DistributionControllerAdapters.SetNode(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.GetOption,
            DistributionRegistrationNames.GetOptionArity,
            (context, arguments) => ValueTask.FromResult(
                DistributionControllerAdapters.GetOption(context, arguments[0], arguments[1])
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.SetOption,
            DistributionRegistrationNames.SetOptionArity,
            (context, arguments) => ValueTask.FromResult(
                DistributionControllerAdapters.SetOption(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                )
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.RequestDataNotification,
            DistributionRegistrationNames.RequestDataNotificationArity,
            (context, arguments) => ValueTask.FromResult(
                DistributionControllerAdapters.RequestDataNotification(context, arguments[0])
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.GetData,
            DistributionRegistrationNames.GetDataArity,
            (context, arguments) => ValueTask.FromResult(
                DistributionControllerAdapters.GetData(context, arguments[0])
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            DistributionRegistrationNames.SetInputHandler,
            DistributionRegistrationNames.SetInputHandlerArity,
            (context, arguments) => ValueTask.FromResult(
                DistributionControllerAdapters.SetInputHandler(context, arguments[0], arguments[1])
            )
        );
    }
}
