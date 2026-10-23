using System.Globalization;
using System.Text;

namespace Erlang;

internal sealed record NifNativeLoadReason(
    NifNativeLoadReasonKind Kind,
    string CallingModule,
    string LibraryModule,
    string Function,
    uint Arity,
    int ReturnCode
)
{
    public string Category => Kind == NifNativeLoadReasonKind.LoadVeto
        ? NifNativeDefaultLoaderNames.Load : NifNativeDefaultLoaderNames.BadLibrary;

    internal string Format(ProcessRuntime runtime)
    {
        if (Kind == NifNativeLoadReasonKind.LoadVeto)
            return NifNativeLoadReasonText.VetoPrefix + ReturnCode.ToString(CultureInfo.InvariantCulture) + NifNativeLoadReasonText.VetoSuffix;

        ReadOnlySpan<byte> displayed = new TermDisplayWriter(runtime).Write(Term.A(CallingModule));
        if (displayed.IsEmpty || displayed[^1] != TermDisplayParameters.LineFeed)
            throw new InvalidOperationException(NifNativeLoadReasonText.DisplayTerminator);
        string module = Encoding.UTF8.GetString(displayed[..^1]);
        if (Kind == NifNativeLoadReasonKind.ModuleMismatch)
            return NifNativeLoadReasonText.ModulePrefix + LibraryModule + NifNativeLoadReasonText.CallingModulePrefix + module + NifNativeLoadReasonText.CallingModuleSuffix;

        string prefix = Kind switch
        {
            NifNativeLoadReasonKind.MissingFunction => NifNativeLoadReasonText.MissingFunctionPrefix,
            NifNativeLoadReasonKind.UndeclaredFunction => NifNativeLoadReasonText.UndeclaredFunctionPrefix,
            NifNativeLoadReasonKind.DuplicateFunction => NifNativeLoadReasonText.DuplicateFunctionPrefix,
            _ => throw new NotSupportedException()
        };

        return prefix + module + NifNativeLoadReasonText.ModuleFunctionSeparator + Function + NifNativeLoadReasonText.FunctionAritySeparator + Arity.ToString(CultureInfo.InvariantCulture);
    }
}
