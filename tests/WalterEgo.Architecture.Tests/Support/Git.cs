namespace WalterEgo.Architecture.Tests.Support;

internal static class Git
{
    /// <summary>Tracked files plus untracked files that are not ignored: everything that would be versioned.</summary>
    public static async Task<IReadOnlyList<string>> VersionedFilesAsync(CancellationToken cancellationToken)
    {
        var result = await RunAsync(["ls-files", "--cached", "--others", "--exclude-standard", "-z"], cancellationToken);
        return result.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    public static async Task<IReadOnlyList<string>> TrackedFilesAsync(string pathspec, CancellationToken cancellationToken)
    {
        var result = await RunAsync(["ls-files", "-z", "--", pathspec], cancellationToken);
        return result.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    public static async Task<bool> IsIgnoredAsync(string path, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("git", ["check-ignore", "-q", "--", path], cancellationToken);
        return result.ExitCode switch
        {
            0 => true,
            1 => false,
            _ => throw new InvalidOperationException($"git check-ignore failed: {result.StandardError}"),
        };
    }

    private static async Task<ProcessResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var result = await ProcessRunner.RunAsync("git", arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {result.StandardError}");
        }

        return result;
    }
}
