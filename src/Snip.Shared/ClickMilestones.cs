namespace Snip.Shared;

public static class ClickMilestones
{
    private static readonly long[] Milestones = { 10, 50, 100, 500, 1000, 5000, 10000, 50000, 100000 };

    public static bool IsMilestone(long previous, long current)
    {
        return Milestones.Any(m => previous < m && current >= m);
    }

    public static long? GetCrossedMilestone(long previous, long current)
    {
        return Milestones.Cast<long?>().FirstOrDefault(m => previous < m && current >= m);
    }
}