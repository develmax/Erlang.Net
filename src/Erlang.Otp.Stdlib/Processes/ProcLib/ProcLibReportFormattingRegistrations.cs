namespace Erlang.Otp;

internal static class ProcLibReportFormattingRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ProcLibNames.Module,
            ProcLibReportFormattingNames.Format,
            1,
            (context, arguments) => Create(context).Format(arguments[0])
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibReportFormattingNames.Format,
            2,
            (context, arguments) => Create(context).Format(arguments[0], arguments[1])
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibReportFormattingNames.Format,
            3,
            (context, arguments) => Create(context).Format(arguments[0], arguments[1], arguments[2])
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibReportFormattingNames.ReportCallback,
            2,
            (context, arguments) => Create(context).ReportCallback(arguments[0], arguments[1])
        );
    }

    private static ProcLibReportFormatting Create(ProcessContext context)
    {
        var printer = new ProcLibReportPrettyPrinter(context.Runtime);
        var exception = new ProcLibReportExceptionFormatter(context, printer);
        var primitives = new ProcLibReportPrimitives(printer, exception.Render);

        return new ProcLibReportFormatting(primitives);
    }
}
