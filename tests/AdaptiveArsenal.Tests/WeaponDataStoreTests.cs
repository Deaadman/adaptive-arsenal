using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class WeaponDataStoreTests
{
    private const string Rifle = "GEAR_Rifle";
    private const string Revolver = "GEAR_Revolver";

    [Fact]
    public void Getting_a_new_weapon_adds_empty_data_for_it()
    {
        var store = new WeaponDataStore();

        var data = store.GetOrAdd(1, Rifle);

        Assert.Equal(Rifle, data.m_GearName);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Getting_a_weapon_again_returns_the_same_data()
    {
        var store = new WeaponDataStore();

        Assert.Same(store.GetOrAdd(1, Rifle), store.GetOrAdd(1, Rifle));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Trying_to_get_a_weapon_with_no_data_doesnt_add_it()
    {
        var store = new WeaponDataStore();

        Assert.False(store.TryGet(1, Rifle, out var data));
        Assert.Null(data);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void A_different_item_with_the_same_instance_id_doesnt_get_the_old_data()
    {
        var store = new WeaponDataStore();
        var rifleData = store.GetOrAdd(1, Rifle);

        Assert.False(store.TryGet(1, Revolver, out _));

        var revolverData = store.GetOrAdd(1, Revolver);
        Assert.NotSame(rifleData, revolverData);
        Assert.Equal(Revolver, revolverData.m_GearName);
        Assert.False(store.TryGet(1, Rifle, out _));
    }

    [Fact]
    public void Removing_a_weapon_forgets_it()
    {
        var store = new WeaponDataStore();
        store.GetOrAdd(1, Rifle);

        Assert.True(store.Remove(1));
        Assert.False(store.Remove(1));
        Assert.False(store.TryGet(1, Rifle, out _));
    }

    [Fact]
    public void Saved_data_loads_back_the_same()
    {
        var saved = new WeaponDataStore();
        saved.GetOrAdd(1, Rifle);
        saved.GetOrAdd(-42, Revolver);

        var loaded = new WeaponDataStore();
        Assert.True(loaded.Deserialize(saved.Serialize()));

        Assert.Equal(2, loaded.Count);
        Assert.True(loaded.TryGet(1, Rifle, out _));
        Assert.True(loaded.TryGet(-42, Revolver, out _));
    }

    [Fact]
    public void Loading_replaces_what_was_there()
    {
        var store = new WeaponDataStore();
        store.GetOrAdd(1, Rifle);

        Assert.True(store.Deserialize(new WeaponDataStore().Serialize()));

        Assert.Equal(0, store.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void A_game_with_nothing_saved_loads_empty(string? json)
    {
        var store = new WeaponDataStore();
        store.GetOrAdd(1, Rifle);

        Assert.True(store.Deserialize(json));
        Assert.Equal(0, store.Count);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Unreadable_data_loads_empty_and_says_so(string json)
    {
        var store = new WeaponDataStore();
        store.GetOrAdd(1, Rifle);

        Assert.False(store.Deserialize(json));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void New_instance_ids_start_above_unitys_and_are_never_repeated()
    {
        var store = new WeaponDataStore();

        var first = store.NewInstanceId();
        var second = store.NewInstanceId();

        Assert.Equal(WeaponDataStore.FirstInstanceId, first);
        Assert.Equal(first + 1, second);
    }

    [Fact]
    public void New_instance_ids_skip_ones_that_have_data()
    {
        var store = new WeaponDataStore();
        store.GetOrAdd(WeaponDataStore.FirstInstanceId, Rifle);

        Assert.Equal(WeaponDataStore.FirstInstanceId + 1, store.NewInstanceId());
    }

    [Fact]
    public void The_next_instance_id_is_saved_and_loaded()
    {
        // A gun given an ID but with no data yet must not share it with one made after loading.
        var saved = new WeaponDataStore();
        var handedOut = saved.NewInstanceId();

        var loaded = new WeaponDataStore();
        Assert.True(loaded.Deserialize(saved.Serialize()));

        Assert.Equal(handedOut + 1, loaded.NewInstanceId());
    }

    [Fact]
    public void Saves_from_before_the_next_instance_id_was_saved_start_from_the_first()
    {
        const string json = """{"m_Version":1,"m_Weapons":{"-42":{"m_GearName":"GEAR_Rifle"}}}""";
        var store = new WeaponDataStore();
        store.NewInstanceId();

        Assert.True(store.Deserialize(json));

        Assert.Equal(WeaponDataStore.FirstInstanceId, store.NewInstanceId());
    }

    [Fact]
    public void Clearing_starts_instance_ids_from_the_first_again()
    {
        var store = new WeaponDataStore();
        store.NewInstanceId();

        store.Clear();

        Assert.Equal(WeaponDataStore.FirstInstanceId, store.NewInstanceId());
    }

    [Fact]
    public void Fields_it_doesnt_know_are_ignored()
    {
        // As when a save from a newer version of the mod, with more fields, is loaded by an older one.
        const string json = """{"m_Version":99,"m_Weapons":{"1":{"m_GearName":"GEAR_Rifle","m_Unknown":5}},"m_Other":true}""";
        var store = new WeaponDataStore();

        Assert.True(store.Deserialize(json));
        Assert.True(store.TryGet(1, Rifle, out _));
    }
}
