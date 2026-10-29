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
    internal int Major651 { get; init; }
    internal int Minor651 { get; init; }
    internal string VersionText651 { get; init; } = string.Empty;

    internal uint Flags521 { get; init; }

    public string Category => Kind == NifNativeLoadReasonKind.LoadVeto
        ? NifNativeDefaultLoaderNames.Load : NifNativeDefaultLoaderNames.BadLibrary;

    internal string Format(ProcessRuntime runtime)
    {
        if (Kind == NifNativeLoadReasonKind.LoadVeto)
            return NifNativeLoadReasonText.VetoPrefix + ReturnCode.ToString(CultureInfo.InvariantCulture) + NifNativeLoadReasonText.VetoSuffix;

        if (Kind == NifNativeLoadReasonKind.OldVersion651)
            return NifNativeVersion651.OldPrefix + Major651.ToString(CultureInfo.InvariantCulture)
                + NifNativeVersion651.VersionSeparator + Minor651.ToString(CultureInfo.InvariantCulture)
                + NifNativeVersion651.OldMiddle + NifNativeAbi.MajorVersion.ToString(CultureInfo.InvariantCulture)
                + NifNativeVersion651.VersionSeparator + NifNativeAbi.MinorVersion.ToString(CultureInfo.InvariantCulture)
                + NifNativeVersion651.OldSuffix;
        if (Kind == NifNativeLoadReasonKind.Variant651)
            return NifNativeVersion651.VariantPrefix + VersionText651 + NifNativeVersion651.VariantMiddle
                + NifNativeNames.Variant + NifNativeVersion651.VariantSuffix;

        ReadOnlySpan<byte> displayed = new TermDisplayWriter(runtime).Write(Term.A(CallingModule));
        if (displayed.IsEmpty || displayed[^1] != TermDisplayParameters.LineFeed)
            throw new InvalidOperationException(NifNativeLoadReasonText.DisplayTerminator);
        // load_nif_error builds its charlist directly from the formatter bytes.
        string module = Encoding.Latin1.GetString(displayed[..^1]);
        if (Kind == NifNativeLoadReasonKind.FutureVersion651)
            return NifNativeVersion651.FuturePrefix + module + NifNativeVersion651.FutureMiddle
                + VersionText651 + NifNativeVersion651.FutureSuffix;
        if (Kind == NifNativeLoadReasonKind.ModuleMismatch)
            return NifNativeLoadReasonText.ModulePrefix + LibraryModule + NifNativeLoadReasonText.CallingModulePrefix + module + NifNativeLoadReasonText.CallingModuleSuffix;

        if (Kind == NifNativeLoadReasonKind.IllegalFlags521)
            return NifNativeFlagReason521Names.Prefix + FormatFlags521()
                + NifNativeFlagReason521Names.ForNif + module + NifNativeLoadReasonText.ModuleFunctionSeparator
                + Function + NifNativeLoadReasonText.FunctionAritySeparator + Arity.ToString(CultureInfo.InvariantCulture);

        string prefix = Kind switch
        {
            NifNativeLoadReasonKind.MissingFunction => NifNativeLoadReasonText.MissingFunctionPrefix,
            NifNativeLoadReasonKind.UndeclaredFunction => NifNativeLoadReasonText.UndeclaredFunctionPrefix,
            NifNativeLoadReasonKind.DuplicateFunction => NifNativeLoadReasonText.DuplicateFunctionPrefix,
            _ => throw new NotSupportedException()
        };

        return prefix + module + NifNativeLoadReasonText.ModuleFunctionSeparator + Function + NifNativeLoadReasonText.FunctionAritySeparator + Arity.ToString(CultureInfo.InvariantCulture);
    }

    private string FormatFlags521()
    {
        int value = unchecked((int)Flags521);
        if (value >= 0)
            return value.ToString(CultureInfo.InvariantCulture);

        // Pinned erl_printf_format.c negates the signed int before converting
        // to ErlPfUWord, including the native INT_MIN wraparound.
        nuint magnitude = unchecked((nuint)(nint)(-value));

        return CultureInfo.InvariantCulture.NumberFormat.NegativeSign + magnitude.ToString(CultureInfo.InvariantCulture);
    }

}
