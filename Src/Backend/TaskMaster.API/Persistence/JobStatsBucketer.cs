using System.Globalization;
using TaskMaster.API.Models.Dashboard;

namespace TaskMaster.API.Persistence
{
    /// <summary>
    /// Computes the 2-hour bucket series for a caller-specified IANA timezone. All wall-clock
    /// arithmetic happens here (DST-aware via <see cref="TimeZoneInfo"/>); the result is a list
    /// of UTC boundary instants plus precomputed local labels that SQL stores aggregate against,
    /// so a single GROUP BY query is sufficient and no instants are ever pulled into memory.
    /// </summary>
    public static class JobStatsBucketer
    {
        public const int BucketSizeMinutes = 120;
        public const int BucketCount = 12;

        public static IReadOnlyList<JobStatsItem> CreateBuckets(TimeZoneInfo timeZone, DateTime nowUtc)
        {
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(ToUtc(nowUtc), timeZone);
            var currentBucketLocal = FloorToBucketStart(nowLocal);

            var buckets = new List<JobStatsItem>(BucketCount);
            for (var i = 0; i < BucketCount; i++)
            {
                var localStart = currentBucketLocal.AddHours(-((BucketCount - 1) * 2) + (i * 2));
                var localEnd = localStart.AddHours(2);

                buckets.Add(new JobStatsItem
                {
                    BucketStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
                    BucketEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone),
                    Label = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                    JobCount = 0
                });
            }

            return buckets;
        }

        public static JobStatsResponse BuildResponse(
            IReadOnlyList<JobStatsItem> buckets,
            IReadOnlyDictionary<int, int> counts,
            TimeZoneInfo timeZone)
        {
            for (var i = 0; i < buckets.Count; i++)
            {
                buckets[i].JobCount = counts.TryGetValue(i, out var count) ? count : 0;
            }

            return new JobStatsResponse
            {
                Timezone = timeZone.Id,
                WindowStartUtc = buckets[0].BucketStart,
                WindowEndUtc = buckets[buckets.Count - 1].BucketEnd,
                BucketSizeMinutes = BucketSizeMinutes,
                Buckets = buckets
            };
        }

        private static DateTime FloorToBucketStart(DateTime local)
        {
            var flooredHour = local.Hour - (local.Hour % 2);
            return new DateTime(local.Year, local.Month, local.Day, flooredHour, 0, 0, DateTimeKind.Unspecified);
        }

        private static DateTime ToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc or DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => value
            };
        }
    }
}