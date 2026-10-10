using TaskMaster.API.Models.Dashboard;
using TaskMaster.API.Persistence;

namespace TaskMaster.Test.UnitTests.APITests;

public class JobStatsBucketerTests
{
    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    private static readonly TimeZoneInfo Tokyo = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Test]
    public void CreateBuckets_WhenCalled_ShouldReturnTwelveEmptyBuckets()
    {
        var nowUtc = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);

        var buckets = JobStatsBucketer.CreateBuckets(London, nowUtc);

        Assert.Multiple(() =>
        {
            Assert.That(buckets, Has.Count.EqualTo(12));
            Assert.That(buckets.All(b => b.JobCount == 0), Is.True);
        });
    }

    [Test]
    public void CreateBuckets_ShouldAlignToLocalEvenHours()
    {
        var nowLocal = new DateTime(2026, 7, 11, 13, 0, 0); // Saturday, BST active (UTC+1)
        var nowUtc = TimeZoneInfo.ConvertTimeToUtc(nowLocal, London);

        var buckets = JobStatsBucketer.CreateBuckets(London, nowUtc);

        Assert.Multiple(() =>
        {
            Assert.That(buckets[0].Label, Is.EqualTo("14:00"));
            Assert.That(buckets[^1].Label, Is.EqualTo("12:00"));
            Assert.That(buckets, Is.Ordered.Ascending.By(nameof(JobStatsItem.BucketStart)));
        });

        for (var i = 0; i < buckets.Count - 1; i++)
        {
            Assert.That(buckets[i + 1].BucketStart - buckets[i].BucketStart, Is.EqualTo(TimeSpan.FromHours(2)), "Buckets on a non-DST day must be exactly two hours apart");
        }
    }

    [Test]
    public void CreateBuckets_ShouldReturnUtcInstantsForWire()
    {
        var nowUtc = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);
        var buckets = JobStatsBucketer.CreateBuckets(Tokyo, nowUtc);

        foreach (var bucket in buckets)
        {
            Assert.That(bucket.BucketStart.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(bucket.BucketEnd.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }

    [Test]
    public void CreateBuckets_AcrossSpringForward_ShouldLabelGapDayCorrectly()
    {
        // Europe/London: on 29 Mar 2026 at 01:00Z clocks spring forward to 02:00 local.
        var springForwardInstantUtc = new DateTime(2026, 3, 29, 2, 30, 0, DateTimeKind.Utc); // 03:30 local

        var buckets = JobStatsBucketer.CreateBuckets(London, springForwardInstantUtc);

        Assert.Multiple(() =>
        {
            Assert.That(buckets, Has.Count.EqualTo(12));
            Assert.That(buckets[^1].Label, Is.EqualTo("02:00"));
        });
    }

    [Test]
    public void CreateBuckets_AcrossFallBack_ShouldSpanThreeHoursForRepeatedLocalHour()
    {
        // Europe/London: on 25 Oct 2026 at 01:00Z clocks fall back from 02:00 to 01:00 local,
        // so the local hour 01:00 occurs twice and the 00:00-02:00 local bucket spans 3h in UTC.
        var nowUtc = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc); // 12:00 GMT

        var buckets = JobStatsBucketer.CreateBuckets(London, nowUtc);

        var repeatedHourBucket = buckets.Single(b => b.Label == "00:00");
        Assert.That(repeatedHourBucket.BucketEnd - repeatedHourBucket.BucketStart, Is.EqualTo(TimeSpan.FromHours(3)));
    }

    [Test]
    public void BuildResponse_WhenCountsProvided_ShouldFillCountsAndWindow()
    {
        var nowUtc = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);
        var buckets = JobStatsBucketer.CreateBuckets(Utc, nowUtc);
        var counts = new Dictionary<int, int> { [1] = 5, [3] = 3 };

        var response = JobStatsBucketer.BuildResponse(buckets, counts, Utc);

        Assert.Multiple(() =>
        {
            Assert.That(response.Timezone, Is.EqualTo("UTC"));
            Assert.That(response.BucketSizeMinutes, Is.EqualTo(120));
            Assert.That(response.Buckets, Has.Count.EqualTo(12));
            Assert.That(response.Buckets[1].JobCount, Is.EqualTo(5));
            Assert.That(response.Buckets[3].JobCount, Is.EqualTo(3));
            Assert.That(response.Buckets.Count(b => b.JobCount == 0), Is.EqualTo(10));
            Assert.That(response.WindowStartUtc, Is.EqualTo(buckets[0].BucketStart));
            Assert.That(response.WindowEndUtc, Is.EqualTo(buckets[^1].BucketEnd));
        });
    }

    [Test]
    public void BuildResponse_WhenBucketIndexCounted_ShouldMapByIndexNotOrder()
    {
        var nowUtc = new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);
        var buckets = JobStatsBucketer.CreateBuckets(London, nowUtc);

        var response = JobStatsBucketer.BuildResponse(buckets, new Dictionary<int, int> { [8] = 7 }, London);

        Assert.That(response.Buckets[8].JobCount, Is.EqualTo(7));
        Assert.That(response.Buckets[0].JobCount, Is.EqualTo(0));
    }
}