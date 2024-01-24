using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using IniParser;

namespace AwsCliCredentialUtility;

internal class MainWindowViewModel
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    public RelayCommand<string> Command { get; } = new(RefreshShortTermCredential);

    private static void RefreshShortTermCredential(string? profile)
    {
        if (profile == null) return;

        var ssoProfile = GetSsoProfile(profile);

        RunAwsCli(profile);

        var credentialFilePath = GetLastModifiedCredentialCache();

        var credentialCache = GetCredentialCache(credentialFilePath);

        var credential = GetShortTermCredential(ssoProfile, credentialCache);

        SaveCredential(ssoProfile, credential);
    }

    private static void SaveCredential(SsoProfile ssoProfile, Credential credential)
    {
        var path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "credentials");
        var parser = new FileIniDataParser();
        var config = parser.ReadFile(path);
        var section = config[$"{ssoProfile.SsoAccountId}_{ssoProfile.SsoRoleName}"];
        section["aws_access_key_id"] = credential.AccessKeyId;
        section["aws_secret_access_key"] = credential.SecretAccessKey;
        section["aws_session_token"] = credential.SessionToken;
        parser.WriteFile(path, config, new UTF8Encoding(false));
    }

    private static Credential GetShortTermCredential(SsoProfile ssoProfile, CredentialCache credentialCache)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", credentialCache.AccessToken);

        var response = httpClient.GetAsync(
                $"https://portal.sso.ap-northeast-2.amazonaws.com/federation/credentials/?account_id={ssoProfile.SsoAccountId}&role_name={ssoProfile.SsoRoleName}&debug=true")
            .Result;

        if (!response.IsSuccessStatusCode) throw new Exception("임시 보안 자격 발급 실패");

        return JsonSerializer.Deserialize<CredentialResponse>(response.Content.ReadAsStringAsync().Result,
            JsonSerializerOptions)!.RoleCredentials;
    }

    private static CredentialCache GetCredentialCache(string credentialFilePath)
    {
        var json = File.ReadAllText(credentialFilePath);
        return JsonSerializer.Deserialize<CredentialCache>(json, JsonSerializerOptions)!;
    }

    private static string GetLastModifiedCredentialCache()
    {
        var directory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "sso",
            "cache");
        var files = Directory.GetFiles(directory);
        return files.OrderByDescending(File.GetLastWriteTime).First();
    }

    private static void RunAwsCli(string profile)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "aws",
            Arguments = $"sso login --profile {profile}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        Process.Start(startInfo)?.StandardOutput.ReadToEnd();
    }

    private static SsoProfile GetSsoProfile(string profile)
    {
        var path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "config");
        var parser = new FileIniDataParser();
        var config = parser.ReadFile(path);

        var section = config[$"profile {profile}"];
        var ssoAccountId = section["sso_account_id"];
        var ssoRoleName = section["sso_role_name"];

        if (ssoAccountId is null || ssoRoleName is null) throw new Exception("sso profile을 읽을 수 없음");

        return new SsoProfile(ssoAccountId, ssoRoleName);
    }

    private record CredentialResponse(Credential RoleCredentials);

    [Serializable]
    private record Credential(string AccessKeyId, string SecretAccessKey, string SessionToken);

    private record CredentialCache(string AccessToken);

    private record SsoProfile(string SsoAccountId, string SsoRoleName);
}