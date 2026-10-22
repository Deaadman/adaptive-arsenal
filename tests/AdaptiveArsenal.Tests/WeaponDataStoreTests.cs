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
    public void A_weapons_skin_is_saved_and_its_own_look_is_left_out()
    {
        var saved = new WeaponDataStore();
        saved.GetOrAdd(1, Rifle).m_Skin = "GEAR_Rifle/Arctic";
        saved.GetOrAdd(2, Rifle);
        var json = saved.Serialize();

        var loaded = new WeaponDataStore();
        Assert.True(loaded.Deserialize(json));

        Assert.True(loaded.TryGet(1, Rifle, out var skinned));
        Assert.Equal("GEAR_Rifle/Arctic", skinned.m_Skin);
        Assert.True(loaded.TryGet(2, Rifle, out var plain));
        Assert.Null(plain.m_Skin);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(json, "m_Skin"));
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
    public void Fields_it_doesnt_know_are_ignored()
    {
        // As when a save from a newer version of the mod, with more fields, is loaded by an older one.
        const string json = """{"m_Version":99,"m_Weapons":{"1":{"m_GearName":"GEAR_Rifle","m_Unknown":5}},"m_Other":true}""";
        var store = new WeaponDataStore();

        Assert.True(store.Deserialize(json));
        Assert.True(store.TryGet(1, Rifle, out _));
    }
}
