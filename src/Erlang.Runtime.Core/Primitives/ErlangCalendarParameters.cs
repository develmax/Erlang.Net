namespace Erlang;

internal static class ErlangCalendarParameters
{
    public const int EarliestEpochYear = 1600;
    public const int EarliestLocalYear = 1902;
    public const int MaximumInputYear = int.MaxValue - 1;
    public const long MinimumSmallComponent = -(1L << 59);
    public const long MaximumSmallComponent = (1L << 59) - 1;
    public const long SecondsPerMinute = 60;
    public const long SecondsPerHour = 3600;
    public const long SecondsPerDay = 86400;
    public const long EpochGregorianDays = 135140;
    public const long MarchCivilEpochDays = 719468;
    public const long GregorianApproximationMultiplier = 10000;
    public const long GregorianApproximationOffset = 14780;
    public const long GregorianApproximationDivisor = 3652425;
    public const long MonthApproximationMultiplier = 100;
    public const long MonthApproximationOffset = 52;
    public const long MonthApproximationDivisor = 3060;
    public const long MarchMonthDaysScale = 306;
    public const long MarchMonthDaysOffset = 5;
    public const long MarchMonthDaysDivisor = 10;
    public const int GregorianCycleYears = 400;
    public const int FutureMappingBaseYear = 2400;
    public const int OrdinaryYearDays = 365;
    public const int LeapYearDays = 366;
    public const int LeapYearInterval = 4;
    public const int GregorianCenturyYears = 100;
    public const int MonthsPerYear = 12;
    public const int February = 2;
    public const int MarchToJanuaryOffset = 2;
    public const int MaximumHour = 23;
    public const int MaximumMinute = 59;
    public const int MaximumSecond = 59;
    public static IReadOnlyList<int> MonthDays { get; } = Array.AsReadOnly(new[] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 });
}
