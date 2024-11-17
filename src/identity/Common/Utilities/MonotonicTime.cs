// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Utilities;

/// <summary>
/// A helper class for calculating time elapsed in a code path.
/// </summary>
/// <remarks>
/// Clock drift. https://blog.cloudflare.com/how-and-why-the-leap-second-affected-cloudflare-dns/
/// Here’s an interesting discussion: <a href="https://github.com/dotnet/corefx/issues/3249" />
/// Stopwatch.GetTimestamp() is indeed the way to go when profiling large quantities of code.
/// It prevents the additional heap allocations and GC you'd have with lots of Stopwatch
/// instances. Be sure to take the Frequency property into account when computing the result.
/// Further points on why DateTime.Now based calculation is not recommended:
/// The other two are:
/// 1. It is slow to compute and it allocates memory.This is mostly because of the local
/// time zone adjustment. <c>DateTime.UtcNow</c> is faster, but it still suffers from
/// discontinuity.
/// 2. It is inaccurate.Only on very recent builds of CoreCLR have we moved to a more
/// <a href="https://github.com/dotnet/coreclr/pull/9736">accurate API</a>.
/// (Note: more accurate.
/// The precision is the same. See:
/// https://blogs.msdn.microsoft.com/ericlippert/2010/04/08/precision-and-accuracy-of-datetime/.
/// </remarks>
[Serializable]
public readonly struct MonotonicTime
{
    /// <summary>
    /// Stopwatch Ticks to timespan ticks.
    /// </summary>
    private static readonly double StopwatchTicksToTimeSpanTicks =
        (double)TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency;

    /// <summary>
    /// The current ticks value.
    /// </summary>
    private readonly long ticks;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonotonicTime" /> struct.
    /// </summary>
    /// <param name="ticksInput">The input ticks.</param>
    private MonotonicTime(long ticksInput)
    {
        this.ticks = ticksInput;
    }

    /// <summary>
    /// Gets a MonotonicTime object.
    /// </summary>
    public static MonotonicTime Now =>
        new(System.Diagnostics.Stopwatch.GetTimestamp());

    /// <summary>
    /// Used to determine the time elapsed between two MonotonicTime objects.
    /// </summary>
    /// <param name="end">The end time.</param>
    /// <param name="start">The start time.</param>
    /// <returns>A TimeSpan value.</returns>
    public static TimeSpan operator -(MonotonicTime end, MonotonicTime start) =>
        TimeSpan.FromTicks(ToTimespanTicks(end.ticks - start.ticks));

    /// <summary>
    /// Used to add timespan to MonotonicTime objects.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="offset">The offset time.</param>
    /// <returns>The monotonic time.</returns>
    public static MonotonicTime operator +(MonotonicTime start, TimeSpan offset) =>
        new(start.ticks + ToStopwatchTicks(offset.Ticks));

    /// <summary>
    /// Used to compare two MonotonicTime objects.
    /// </summary>
    /// <param name="left">The left operand Monotonic time for comparison.</param>
    /// <param name="right">The right operand Monotonic time for comparison.</param>
    /// <returns>The monotonic time.</returns>
    public static bool operator <=(MonotonicTime left, MonotonicTime right) =>
        left.ticks <= right.ticks;

    /// <summary>
    /// Used to compare two MonotonicTime objects.
    /// </summary>
    /// <param name="left">The left operand Monotonic time for comparison.</param>
    /// <param name="right">The right operand Monotonic time for comparison.</param>
    /// <returns>The monotonic time.</returns>
    public static bool operator >=(MonotonicTime left, MonotonicTime right) =>
        left.ticks >= right.ticks;

    /// <summary>
    /// Helper method for conversion.
    /// </summary>
    /// <param name="stopwatchTicks">The ticks per StopWatch.</param>
    /// <returns>TimeSpan ticks.</returns>
    private static long ToTimespanTicks(long stopwatchTicks)
    {
        return (long)(stopwatchTicks * StopwatchTicksToTimeSpanTicks);
    }

    /// <summary>
    /// Helper method for conversion.
    /// </summary>
    /// <param name="timespanTicks">The ticks per timespan.</param>
    /// <returns>TimeSpan ticks.</returns>
    private static long ToStopwatchTicks(long timespanTicks)
    {
        return (long)(timespanTicks / StopwatchTicksToTimeSpanTicks);
    }
}
