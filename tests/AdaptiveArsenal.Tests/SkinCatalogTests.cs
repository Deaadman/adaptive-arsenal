using AdaptiveArsenal.Core;

namespace AdaptiveArsenal.Tests;

public sealed class SkinCatalogTests : IDisposable
{
    private readonly string m_Root = Path.Combine(Path.GetTempPath(), $"AdaptiveArsenal.Tests.{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(m_Root))
            Directory.Delete(m_Root, recursive: true);
    }

    [Fact]
    public void A_missing_skins_folder_has_no_skins()
    {
        var skins = SkinCatalog.Discover(m_Root, out var problems);

        Assert.Empty(skins);
        Assert.Empty(problems);
    }

    [Fact]
    public void A_complete_skin_is_found_with_its_id_name_and_images()
    {
        var folder = AddSkin("GEAR_Rifle", "Arctic", """{"name": "Arctic Camo"}""");

        var skin = Assert.Single(SkinCatalog.Discover(m_Root, out var problems));

        Assert.Empty(problems);
        Assert.Equal("GEAR_Rifle/Arctic", skin.Id);
        Assert.Equal("GEAR_Rifle", skin.GearName);
        Assert.Equal("Arctic", skin.FolderName);
        Assert.Equal("Arctic Camo", skin.Name);
        Assert.Equal(Path.Combine(folder, SkinCatalog.TextureFileName), skin.TexturePath);
        Assert.Equal(Path.Combine(folder, SkinCatalog.IconFileName), skin.IconPath);
    }

    [Fact]
    public void Skins_are_found_for_every_weapon_and_sorted_by_id()
    {
        AddSkin("GEAR_Rifle", "Woodland", """{"name": "Woodland"}""");
        AddSkin("GEAR_Revolver", "Brass", """{"name": "Brass"}""");
        AddSkin("GEAR_Rifle", "Arctic", """{"name": "Arctic"}""");

        var ids = SkinCatalog.Discover(m_Root, out _).Select(skin => skin.Id);

        Assert.Equal(["GEAR_Revolver/Brass", "GEAR_Rifle/Arctic", "GEAR_Rifle/Woodland"], ids);
    }

    [Theory]
    [InlineData(SkinCatalog.ManifestFileName)]
    [InlineData(SkinCatalog.TextureFileName)]
    [InlineData(SkinCatalog.IconFileName)]
    public void A_skin_missing_a_file_is_left_out_and_says_which(string missingFile)
    {
        var folder = AddSkin("GEAR_Rifle", "Arctic", """{"name": "Arctic"}""");
        File.Delete(Path.Combine(folder, missingFile));

        Assert.Empty(SkinCatalog.Discover(m_Root, out var problems));
        Assert.Contains(missingFile, Assert.Single(problems));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"name": "  "}""")]
    [InlineData("null")]
    public void A_skin_without_a_readable_name_is_left_out(string manifest)
    {
        AddSkin("GEAR_Rifle", "Arctic", manifest);

        Assert.Empty(SkinCatalog.Discover(m_Root, out var problems));
        Assert.Contains("GEAR_Rifle/Arctic", Assert.Single(problems));
    }

    [Fact]
    public void A_broken_skin_doesnt_stop_the_others_loading()
    {
        AddSkin("GEAR_Rifle", "Broken", "not json");
        AddSkin("GEAR_Rifle", "Arctic", """{"name": "Arctic"}""");

        Assert.Equal("GEAR_Rifle/Arctic", Assert.Single(SkinCatalog.Discover(m_Root, out _)).Id);
    }

    [Fact]
    public void The_name_is_read_whatever_its_case_and_trimmed()
    {
        AddSkin("GEAR_Rifle", "Arctic", """{"Name": " Arctic ", "extra": 1,}""");

        Assert.Equal("Arctic", Assert.Single(SkinCatalog.Discover(m_Root, out _)).Name);
    }

    [Fact]
    public void The_description_and_author_are_read_and_trimmed()
    {
        AddSkin("GEAR_Rifle", "Arctic", """{"name": "Arctic", "description": " White for the snow. ", "author": " Deadman "}""");

        var skin = Assert.Single(SkinCatalog.Discover(m_Root, out _));
        Assert.Equal("White for the snow.", skin.Description);
        Assert.Equal("Deadman", skin.Author);
    }

    [Theory]
    [InlineData("""{"name": "Arctic"}""")]
    [InlineData("""{"name": "Arctic", "description": "  ", "author": ""}""")]
    [InlineData("""{"name": "Arctic", "description": null, "author": null}""")]
    public void A_skin_without_a_description_or_author_still_loads_without_them(string manifest)
    {
        AddSkin("GEAR_Rifle", "Arctic", manifest);

        var skin = Assert.Single(SkinCatalog.Discover(m_Root, out var problems));
        Assert.Empty(problems);
        Assert.Null(skin.Description);
        Assert.Null(skin.Author);
    }

    /// <summary>Adds a skin folder with the given manifest and empty images, and returns its path.</summary>
    private string AddSkin(string gearName, string folderName, string manifest)
    {
        var folder = Path.Combine(m_Root, gearName, folderName);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, SkinCatalog.ManifestFileName), manifest);
        File.WriteAllBytes(Path.Combine(folder, SkinCatalog.TextureFileName), []);
        File.WriteAllBytes(Path.Combine(folder, SkinCatalog.IconFileName), []);
        return folder;
    }
}
