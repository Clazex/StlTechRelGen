using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StlTechRelGen;

internal sealed partial class UpdateChecker(Config.UpdateConfig config) {
	private sealed class ReleaseInfo {
		public string TagName { get; set; } = null!;
		// Actually is Uri, but there's no need to validate
		public string HtmlUrl { get; set; } = null!;
		public DateTime CreatedAt { get; set; }
		public string Body { get; set; } = null!;
	}

	[JsonSourceGenerationOptions(
		PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
	)]
	[JsonSerializable(typeof(ReleaseInfo))]
	private sealed partial class JsonContext : JsonSerializerContext {
	}

	private Task<ReleaseInfo?> CheckTask { get; init; } =
		Task.Run(() => CheckVersion(config));

	public void TryReport() {
		if (!config.WaitForCheckBeforeExit && !CheckTask.IsCompletedSuccessfully) {
			return;
		}

		ReleaseInfo? release = CheckTask.Result;
		if (release == null) {
			return;
		}

		AnsiConsole.MarkupLine(Messages.Update.NewVersionFound.FormatLocal(
			release.HtmlUrl, release.TagName, release.CreatedAt
		));
		if (!string.IsNullOrWhiteSpace(release.Body)) {
			Log(Messages.Update.ReleaseBody, release.Body);
		}
	}

	private static async Task<ReleaseInfo?> CheckVersion(
		Config.UpdateConfig config
	) {
		ReleaseInfo? release = await GetLatestRelease(config);
		try {
			return Version.Parse(GlobalInstances.GetProgramVersion())
				< Version.Parse(release?.TagName[1..]!)
				? release : null;
		} catch {
			return null; // Malformed version string — suppress notification
		}
	}

	private static async Task<ReleaseInfo?> GetLatestRelease(
		Config.UpdateConfig config
	) {
		HttpRequestMessage request = new(HttpMethod.Get, Constants.CheckUpdateUrl);
		request.Headers.Accept.Add(new("application/vnd.github+json"));
		request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");

		if (config.LastETag is string lastETag) {
			request.Headers.IfNoneMatch.Add(new(lastETag, true));
		}

		if (config.GitHubApiToken is string token) {
			// https://docs.github.com/en/rest/releases/releases?apiVersion=2026-03-10#get-the-latest-release--fine-grained-access-tokens
			request.Headers.Authorization = new("Bearer", token);
		}

		HttpResponseMessage response = await GlobalInstances.Client
			.SendAsync(request);
		if (!response.IsSuccessStatusCode) {
			// In case of 304, there is no need to update ETag record as well
			return null;
		}

		config.LastETag = response.Headers.ETag?.Tag;
		return await response.Content
			.ReadFromJsonAsync(JsonContext.Default.ReleaseInfo);
	}
}
