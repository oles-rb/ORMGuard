namespace ORM.Trace.Processing
{
    internal static class SamplingDecider
    {
        private static readonly ThreadLocal<Random> RandomSource = new(() =>
            new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        public static bool ShouldCapture(double rate) =>
            rate >= 1 || (rate > 0 && RandomSource.Value!.NextDouble() <= rate);
    }
}
