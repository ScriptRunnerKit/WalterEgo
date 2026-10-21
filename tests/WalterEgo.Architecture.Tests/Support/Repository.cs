namespace WalterEgo.Architecture.Tests.Support;

internal static class Repository
{
    private const string SolutionFileName = "WalterEgo.slnx";

    public static string Root { get; } = FindRoot();

    public static string PathOf(string relativePath) => Path.GetFullPath(Path.Combine(Root, relativePath));

    public static string ReadText(string relativePath) => File.ReadAllText(PathOf(relativePath));

    public static string[] ReadLines(string relativePath) => File.ReadAllLines(PathOf(relativePath));

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFileName} was not found above {AppContext.BaseDirectory}.");
    }
}
