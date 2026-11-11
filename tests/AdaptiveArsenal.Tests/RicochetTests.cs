using System.Numerics;

using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public class RicochetTests
{
    private const int Precision = 3;

    /// <summary>Flat ground, facing up.</summary>
    private static readonly Vector3 Ground = Vector3.UnitY;

    /// <summary>A velocity of 400 m/s heading down into the ground at <paramref name="degrees"/>.</summary>
    private static Vector3 Incoming(float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        return new Vector3(MathF.Cos(radians), -MathF.Sin(radians), 0f) * 400f;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(5f)]
    [InlineData(45f)]
    [InlineData(90f)]
    public void The_angle_to_the_surface_is_measured_from_it(float degrees) =>
        Assert.Equal(degrees, Ricochet.AngleToSurface(Incoming(degrees), Ground), Precision);

    [Fact]
    public void A_bullet_that_isnt_moving_has_no_angle() =>
        Assert.Equal(0f, Ricochet.AngleToSurface(Vector3.Zero, Ground));

    [Theory]
    [InlineData(2f)]
    [InlineData(9.9f)]
    public void A_shallow_hit_on_a_hard_surface_glances_off(float degrees) =>
        Assert.True(Ricochet.Glances(Incoming(degrees), Ground, SurfaceKind.Stone, ricochetsSoFar: 0));

    [Theory]
    [InlineData(10.1f)]
    [InlineData(45f)]
    [InlineData(90f)]
    public void A_steep_hit_stops(float degrees) =>
        Assert.False(Ricochet.Glances(Incoming(degrees), Ground, SurfaceKind.Stone, ricochetsSoFar: 0));

    [Fact]
    public void A_soft_surface_never_glances() =>
        Assert.False(Ricochet.Glances(Incoming(2f), Ground, SurfaceKind.Snow, ricochetsSoFar: 0));

    [Theory]
    [InlineData((int)SurfaceKind.Stone, true)]
    [InlineData((int)SurfaceKind.Metal, true)]
    [InlineData((int)SurfaceKind.Ice, true)]
    [InlineData((int)SurfaceKind.Snow, false)]
    [InlineData((int)SurfaceKind.Wood, false)]
    [InlineData((int)SurfaceKind.Fabric, false)]
    [InlineData((int)SurfaceKind.Flesh, false)]
    [InlineData((int)SurfaceKind.Glass, false)]
    [InlineData((int)SurfaceKind.Untagged, false)]
    public void Only_rock_metal_and_ice_are_hard_enough_to_glance_off(int kind, bool glances)
    {
        // xunit needs a public parameter type, and SurfaceKind is internal, so it comes in as a number.
        var surface = (SurfaceKind)kind;
        Assert.Equal(glances, Ricochet.IsHard(surface));
        Assert.Equal(glances, Ricochet.Glances(Incoming(2f), Ground, surface, ricochetsSoFar: 0));
    }

    [Fact]
    public void The_angle_check_alone_doesnt_need_the_surface()
    {
        Assert.True(Ricochet.IsShallow(Incoming(2f), Ground, ricochetsSoFar: 0));
        Assert.False(Ricochet.IsShallow(Incoming(20f), Ground, ricochetsSoFar: 0));
    }

    [Fact]
    public void Every_surface_kind_is_covered()
    {
        // The game's BulletImpactEffectType has these nine, which ProjectileItem maps one to one.
        Assert.Equal(9, Enum.GetValues<SurfaceKind>().Length);
    }

    [Fact]
    public void A_bullet_stops_after_its_last_ricochet()
    {
        Assert.True(Ricochet.Glances(Incoming(2f), Ground, SurfaceKind.Stone, ricochetsSoFar: Ricochet.MaxRicochets - 1));
        Assert.False(Ricochet.Glances(Incoming(2f), Ground, SurfaceKind.Stone, ricochetsSoFar: Ricochet.MaxRicochets));
    }

    [Fact]
    public void A_bullet_moving_away_from_the_surface_doesnt_glance() =>
        Assert.False(Ricochet.Glances(new Vector3(400f, 10f, 0f), Ground, SurfaceKind.Stone, ricochetsSoFar: 0));

    [Fact]
    public void Without_scatter_a_ricochet_mirrors_off_the_surface_at_three_fifths_the_speed()
    {
        var deflected = Ricochet.Deflect(Incoming(5f), Ground, random1: 0.3f, random2: 0f);
        var mirrored = new Vector3(Incoming(5f).X, -Incoming(5f).Y, 0f) * Ricochet.SpeedRetained;

        Assert.Equal(mirrored.X, deflected.X, Precision);
        Assert.Equal(mirrored.Y, deflected.Y, Precision);
        Assert.Equal(0f, deflected.Z, Precision);
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(0.25f, 1f)]
    [InlineData(0.5f, 1f)]
    [InlineData(0.75f, 0.5f)]
    public void A_ricochet_scatters_no_more_than_its_limit(float random1, float random2)
    {
        var deflected = Ricochet.Deflect(Incoming(8f), Ground, random1, random2);
        var mirrored = Vector3.Normalize(new Vector3(Incoming(8f).X, -Incoming(8f).Y, 0f));
        var degrees = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(deflected), mirrored), -1f, 1f)) * 180f / MathF.PI;

        Assert.True(degrees <= Ricochet.ScatterDegrees + 0.01f, $"scattered {degrees}°");
        Assert.Equal(400f * Ricochet.SpeedRetained, deflected.Length(), Precision);
    }

    [Fact]
    public void A_grazing_ricochet_always_leaves_the_surface()
    {
        // At 1°, a full scatter straight down would send it back into the ground.
        for (var random1 = 0f; random1 < 1f; random1 += 0.05f)
        {
            var deflected = Ricochet.Deflect(Incoming(1f), Ground, random1, random2: 1f);
            Assert.True(Ricochet.AngleToSurface(deflected, Ground) >= 0.49f && deflected.Y > 0f, $"went into the ground at {random1}");
        }
    }

    [Fact]
    public void A_ricochet_off_a_tilted_surface_leaves_it()
    {
        var wall = Vector3.Normalize(new Vector3(-1f, 0f, 1f));
        var velocity = Vector3.Normalize(new Vector3(1f, 0f, -0.9f)) * 500f;
        var deflected = Ricochet.Deflect(velocity, wall, 0.4f, 0.7f);

        Assert.True(Vector3.Dot(deflected, wall) > 0f);
    }

    [Fact]
    public void Nothing_to_deflect_gives_no_velocity()
    {
        Assert.Equal(Vector3.Zero, Ricochet.Deflect(Vector3.Zero, Ground, 0.5f, 0.5f));
        Assert.Equal(Vector3.Zero, Ricochet.Deflect(Incoming(5f), Vector3.Zero, 0.5f, 0.5f));
    }
}
