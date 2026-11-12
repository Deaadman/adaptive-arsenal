using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class TriggerPullTests
{
    [Fact]
    public void The_first_pull_is_new()
    {
        var trigger = new TriggerPull();
        trigger.Pull(0);

        Assert.True(trigger.IsNew);
        Assert.False(trigger.IsSpent);
    }

    [Fact]
    public void A_pull_held_from_the_frame_before_is_not_new()
    {
        var trigger = new TriggerPull();
        trigger.Pull(10);
        trigger.Pull(11);

        Assert.False(trigger.IsNew);
    }

    [Fact]
    public void A_pull_after_a_gap_is_new()
    {
        var trigger = new TriggerPull();
        trigger.Pull(10);
        trigger.Pull(12);

        Assert.True(trigger.IsNew);
    }

    [Fact]
    public void A_spent_pull_stays_spent_while_held_and_a_new_pull_is_not()
    {
        var trigger = new TriggerPull();
        trigger.Pull(10);
        trigger.Spend();
        trigger.Pull(11);

        Assert.True(trigger.IsSpent);

        trigger.Pull(20);
        Assert.False(trigger.IsSpent);
    }
}
