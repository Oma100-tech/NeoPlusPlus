using System.Text.Json;

namespace NeoPlusPlusApp;

internal sealed class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = Environment.UserName;
    public HashSet<string> SolvedChallengeIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Points { get; set; }
    public DateTimeOffset LastActive { get; set; } = DateTimeOffset.UtcNow;
}

internal sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _dataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Neo++");
    private string ProfilesDirectory => Path.Combine(_dataDirectory, "Profiles");
    private string ActiveProfilePath => Path.Combine(_dataDirectory, "active-profile.txt");

    public UserProfile Load()
    {
        if (File.Exists(ActiveProfilePath))
        {
            var activeId = File.ReadAllText(ActiveProfilePath).Trim();
            var activePath = GetProfilePath(activeId);
            if (File.Exists(activePath))
            {
                var activeProfile = JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(activePath), JsonOptions);
                if (activeProfile is not null)
                {
                    return activeProfile;
                }
            }
        }

        var profile = LoadLeaderboard().FirstOrDefault() ?? new UserProfile();
        Save(profile);
        return profile;
    }

    public void Save(UserProfile profile)
    {
        Directory.CreateDirectory(ProfilesDirectory);
        File.WriteAllText(GetProfilePath(profile.Id), JsonSerializer.Serialize(profile, JsonOptions));
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(ActiveProfilePath, profile.Id);
    }

    public UserProfile SelectOrCreate(string displayName)
    {
        var existing = LoadLeaderboard().FirstOrDefault(profile =>
            profile.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
        var profile = existing ?? new UserProfile { DisplayName = displayName };
        Save(profile);
        return profile;
    }

    public void RecordSolved(UserProfile profile, Challenge challenge)
    {
        if (profile.SolvedChallengeIds.Add(challenge.Id))
        {
            profile.Points += challenge.Difficulty switch
            {
                "Easy" => 100,
                "Medium" => 250,
                "Hard" => 500,
                _ => 100
            };
        }

        profile.LastActive = DateTimeOffset.UtcNow;
        Save(profile);
    }

    public IReadOnlyList<UserProfile> LoadLeaderboard()
    {
        if (!Directory.Exists(ProfilesDirectory))
        {
            return [];
        }

        return Directory.GetFiles(ProfilesDirectory, "*.json")
            .Select(path => JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(path), JsonOptions))
            .OfType<UserProfile>()
            .ToList();
    }

    private string GetProfilePath(string profileId) => Path.Combine(ProfilesDirectory, profileId + ".json");
}
