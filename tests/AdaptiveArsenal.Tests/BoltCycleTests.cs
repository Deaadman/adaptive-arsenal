using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class BoltCycleTests
{
    private readonly TriggerPull m_Trigger = new();

    /// <summary>Pulls the trigger on a frame and asks the bolt what that does.</summary>
    private TriggerAction Pull(BoltCycle bolt, int frame)
    {
        m_Trigger.Pull(frame);
        return bolt.PullTrigger(m_Trigger);
    }

    [Fact]
    public void A_ready_bolt_fires()
    {
        var bolt = new BoltCycle();
        Assert.Equal(TriggerAction.Fire, Pull(bolt, frame: 10));
    }

    [Fact]
    public void After_a_shot_the_next_pull_cycles_and_the_one_after_fires()
    {
        var bolt = new BoltCycle();
        Pull(bolt, frame: 10);
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        Assert.Equal(TriggerAction.Cycle, Pull(bolt, frame: 50));
        Assert.Equal(BoltPhase.Ready, bolt.Phase);
        Assert.Equal(TriggerAction.Fire, Pull(bolt, frame: 90));
    }

    [Fact]
    public void Keeping_the_trigger_held_after_a_shot_does_not_cycle()
    {
        var bolt = new BoltCycle();
        Pull(bolt, frame: 10);
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        for (var frame = 11; frame < 40; frame++)
            Assert.Equal(TriggerAction.Ignore, Pull(bolt, frame));

        Assert.Equal(BoltPhase.Settling, bolt.Phase);
    }

    [Fact]
    public void Keeping_the_cycling_pull_held_does_not_fire()
    {
        var bolt = new BoltCycle();
        Pull(bolt, frame: 10);
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        Assert.Equal(TriggerAction.Cycle, Pull(bolt, frame: 50));
        for (var frame = 51; frame < 200; frame++)
            Assert.Equal(TriggerAction.Ignore, Pull(bolt, frame));

        Assert.Equal(TriggerAction.Fire, Pull(bolt, frame: 250));
    }

    [Fact]
    public void A_pull_spent_elsewhere_does_not_fire_while_held()
    {
        var bolt = new BoltCycle();
        m_Trigger.Pull(10);
        m_Trigger.Spend();

        for (var frame = 11; frame < 40; frame++)
            Assert.Equal(TriggerAction.Ignore, Pull(bolt, frame));

        Assert.Equal(TriggerAction.Fire, Pull(bolt, frame: 60));
    }

    [Fact]
    public void The_first_pull_after_a_shot_cycles_even_without_an_earlier_pull()
    {
        var bolt = new BoltCycle();
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        Assert.Equal(TriggerAction.Cycle, Pull(bolt, frame: 50));
    }

    [Fact]
    public void The_bolt_is_held_once_the_recoil_is_over()
    {
        var bolt = new BoltCycle();
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        Assert.False(bolt.TryHold(1f + BoltCycle.HoldDelaySeconds / 2f));
        Assert.True(bolt.TryHold(1f + BoltCycle.HoldDelaySeconds));
        Assert.Equal(BoltPhase.Held, bolt.Phase);
        Assert.False(bolt.TryHold(5f));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(5, true)]
    public void An_empty_or_jamming_rifle_is_not_held(int roundsLeft, bool willJam)
    {
        var bolt = new BoltCycle();
        bolt.Fired(time: 1f, roundsLeft, willJam);

        Assert.Equal(BoltPhase.Ready, bolt.Phase);
        Assert.False(bolt.TryHold(5f));
    }

    [Fact]
    public void Releasing_a_held_bolt_says_to_resume_it()
    {
        var bolt = new BoltCycle();
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);
        bolt.TryHold(2f);

        Assert.True(bolt.Release());
        Assert.Equal(BoltPhase.Ready, bolt.Phase);
        Assert.False(bolt.Release());
    }

    [Fact]
    public void Releasing_before_the_hold_cancels_it()
    {
        var bolt = new BoltCycle();
        bolt.Fired(time: 1f, roundsLeft: 5, willJam: false);

        Assert.False(bolt.Release());
        Assert.False(bolt.TryHold(5f));
    }

    [Fact]
    public void A_rifle_holds_one_more_round_in_the_chamber() =>
        Assert.Equal(11, BoltCycle.CapacityWithChamber(10));

    [Theory]
    [InlineData("GEAR_Rifle(Clone)", true)]
    [InlineData("GEAR_Rifle_Curators", true)]
    [InlineData("GEAR_Revolver", false)]
    [InlineData("GEAR_FlareGun", false)]
    public void Only_the_games_rifles_are_bolt_actions(string gearItemName, bool expected) =>
        Assert.Equal(expected, BoltCycle.IsBoltAction(gearItemName));

    [Theory]
    [InlineData(1.5f, 1.2f)]
    [InlineData(0.1f, 0f)]
    public void Cycling_leaves_the_rest_of_the_time_between_shots(float firingRate, float expected) =>
        Assert.Equal(expected, BoltCycle.SecondsUntilNextShot(firingRate), 4);
}
