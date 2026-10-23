namespace Erlang;

internal static class ErlangCalendar
{

    private static bool Leap(long year) => year % ErlangCalendarParameters.LeapYearInterval == 0 && (year % ErlangCalendarParameters.GregorianCenturyYears != 0 || year % ErlangCalendarParameters.GregorianCycleYears == 0);

    public static Term FromSeconds(long seconds)
    {
        long days = Math.DivRem(seconds, ErlangCalendarParameters.SecondsPerDay, out long remainder);
        if (remainder < 0)
        {
            days--;
            remainder += ErlangCalendarParameters.SecondsPerDay;
        }
        long hour = remainder / ErlangCalendarParameters.SecondsPerHour;
        long minute = remainder % ErlangCalendarParameters.SecondsPerHour / ErlangCalendarParameters.SecondsPerMinute;
        long second = remainder % ErlangCalendarParameters.SecondsPerMinute;
        days += ErlangCalendarParameters.MarchCivilEpochDays;
        long year = (ErlangCalendarParameters.GregorianApproximationMultiplier * days + ErlangCalendarParameters.GregorianApproximationOffset) / ErlangCalendarParameters.GregorianApproximationDivisor;
        long withinYear = days - (ErlangCalendarParameters.OrdinaryYearDays * year + year / ErlangCalendarParameters.LeapYearInterval - year / ErlangCalendarParameters.GregorianCenturyYears + year / ErlangCalendarParameters.GregorianCycleYears);
        if (withinYear < 0)
        {
            year--;
            withinYear = days - (ErlangCalendarParameters.OrdinaryYearDays * year + year / ErlangCalendarParameters.LeapYearInterval - year / ErlangCalendarParameters.GregorianCenturyYears + year / ErlangCalendarParameters.GregorianCycleYears);
        }
        long monthIndex = (ErlangCalendarParameters.MonthApproximationMultiplier * withinYear + ErlangCalendarParameters.MonthApproximationOffset) / ErlangCalendarParameters.MonthApproximationDivisor;
        long month = (monthIndex + ErlangCalendarParameters.MarchToJanuaryOffset) % ErlangCalendarParameters.MonthsPerYear + 1;
        year += (monthIndex + ErlangCalendarParameters.MarchToJanuaryOffset) / ErlangCalendarParameters.MonthsPerYear;
        long day = withinYear - (monthIndex * ErlangCalendarParameters.MarchMonthDaysScale + ErlangCalendarParameters.MarchMonthDaysOffset) / ErlangCalendarParameters.MarchMonthDaysDivisor + 1;

        return Term.Tuple(Term.Tuple(Term.I(year), Term.I(month), Term.I(day)), Term.Tuple(Term.I(hour), Term.I(minute), Term.I(second)));
    }

    public static long[] Parts(Term value, int minimumYear)
    {
        if (value is not TupleTerm { Items.Count: 2 } tuple || tuple.Items[0] is not TupleTerm { Items.Count: 3 } date || tuple.Items[1] is not TupleTerm { Items.Count: 3 } time)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        long[] parts = date.Items.Concat(time.Items).Select(item => item is Integer number && number.Value >= ErlangCalendarParameters.MinimumSmallComponent && number.Value <= ErlangCalendarParameters.MaximumSmallComponent ? (long)number.Value : throw new ErlangException(ErlangErrorReasons.BadArgument)).ToArray();
        if (parts[0] < minimumYear || parts[0] > ErlangCalendarParameters.MaximumInputYear || parts[1] < 1 || parts[1] > ErlangCalendarParameters.MonthsPerYear || parts[2] < 1 || parts[2] > ErlangCalendarParameters.MonthDays[(int)parts[1]] + (parts[1] == ErlangCalendarParameters.February && Leap(parts[0]) ? 1 : 0) || parts[3] < 0 || parts[3] > ErlangCalendarParameters.MaximumHour || parts[4] < 0 || parts[4] > ErlangCalendarParameters.MaximumMinute || parts[5] < 0 || parts[5] > ErlangCalendarParameters.MaximumSecond)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return parts;
    }

    private static long Epoch(long[] parts)
    {
        long year = parts[0];
        long gregorianYear = year - ErlangCalendarParameters.EarliestEpochYear;
        long previousYear = gregorianYear - 1;
        long days = gregorianYear > 0 ? previousYear / ErlangCalendarParameters.LeapYearInterval - previousYear / ErlangCalendarParameters.GregorianCenturyYears + previousYear / ErlangCalendarParameters.GregorianCycleYears + previousYear * ErlangCalendarParameters.OrdinaryYearDays + ErlangCalendarParameters.LeapYearDays : 0;
        for (int month = 1; month < parts[1]; month++)
            days += ErlangCalendarParameters.MonthDays[month];
        if (Leap(year) && parts[1] > ErlangCalendarParameters.February)
            days++;
        days += parts[2] - 1 - ErlangCalendarParameters.EpochGregorianDays;

        return days * ErlangCalendarParameters.SecondsPerDay + parts[3] * ErlangCalendarParameters.SecondsPerHour + parts[4] * ErlangCalendarParameters.SecondsPerMinute + parts[5];
    }

    public static Term ToEpoch(Term value) => Term.I(Epoch(Parts(value, ErlangCalendarParameters.EarliestEpochYear)));

    public static Term FromEpoch(Term value) => value is Integer number && number.Value >= long.MinValue && number.Value <= long.MaxValue ? FromSeconds((long)number.Value) : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static DateTime MappedDate(long[] parts)
    {
        int year = parts[0] <= DateTime.MaxValue.Year ? (int)parts[0] : ErlangCalendarParameters.FutureMappingBaseYear + (int)(parts[0] % ErlangCalendarParameters.GregorianCycleYears);

        return new DateTime(
            year,
            (int)parts[1],
            (int)parts[2],
            (int)parts[3],
            (int)parts[4],
            (int)parts[5],
            DateTimeKind.Unspecified
        );
    }

    public static Term ToLocal(Term value)
    {
        long[] parts = Parts(value, ErlangCalendarParameters.EarliestLocalYear);
        DateTime mapped = DateTime.SpecifyKind(MappedDate(parts), DateTimeKind.Utc);
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(mapped);

        return FromSeconds(Epoch(parts) + offset.Ticks / TimeSpan.TicksPerSecond);
    }

    public static Term ToUniversal(IReadOnlyList<Term> arguments)
    {
        string dst = arguments.Count == 1 ? ErlangCalendarNames.AutomaticDst : arguments[1] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False or ErlangCalendarNames.AutomaticDst } option ? option.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);
        long[] parts = Parts(arguments[0], ErlangCalendarParameters.EarliestLocalYear);
        DateTime mapped = MappedDate(parts);
        var zone = TimeZoneInfo.Local;
        TimeSpan offset = zone.GetUtcOffset(mapped);
        var rules = zone.GetAdjustmentRules();
        var rule = rules.FirstOrDefault(rule => rule.DateStart <= mapped.Date && rule.DateEnd >= mapped.Date);
        bool hasDst = rules.LastOrDefault()?.DaylightDelta != TimeSpan.Zero && rules.Length > 0;
        if (dst != ErlangCalendarNames.AutomaticDst || !hasDst)
        {
            if (zone.IsDaylightSavingTime(mapped))
                offset -= rule?.DaylightDelta ?? TimeSpan.Zero;
            if (hasDst && dst == ErlangBooleanAtoms.True)
                offset += rule?.DaylightDelta ?? TimeSpan.Zero;
        }

        return FromSeconds(Epoch(parts) - offset.Ticks / TimeSpan.TicksPerSecond);
    }

    public static Term Current(string operation)
    {
        DateTime time = operation == ErlangCalendarNames.Universal ? DateTime.UtcNow : DateTime.Now;
        Term date = Term.Tuple(Term.I(time.Year), Term.I(time.Month), Term.I(time.Day));
        Term clock = Term.Tuple(Term.I(time.Hour), Term.I(time.Minute), Term.I(time.Second));

        return operation == ErlangCalendarNames.Date ? date : operation == ErlangCalendarNames.Time ? clock : Term.Tuple(date, clock);
    }
}
