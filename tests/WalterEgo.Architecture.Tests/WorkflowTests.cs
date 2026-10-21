using System.Text.RegularExpressions;
using WalterEgo.Architecture.Tests.Support;

namespace WalterEgo.Architecture.Tests;

public sealed partial class WorkflowTests
{
    private const int MaxJobMinutes = 15;

    [Fact]
    public void Every_action_is_pinned_to_a_commit_sha()
    {
        var workflows = WorkflowFile.All();
        workflows.ShouldNotBeEmpty();

        var unpinned = workflows
            .SelectMany(workflow => workflow.ActionReferences().Select(reference => (workflow.RelativePath, reference)))
            .Where(usage => !usage.reference.StartsWith("./", StringComparison.Ordinal) && !PinnedAction().IsMatch(usage.reference))
            .Select(usage => $"{usage.RelativePath}: {usage.reference}")
            .ToList();

        unpinned.ShouldBeEmpty();
    }

    [Fact]
    public void Every_job_has_a_timeout()
    {
        foreach (var workflow in WorkflowFile.All())
        {
            var jobs = workflow.Jobs();
            jobs.ShouldNotBeEmpty($"{workflow.RelativePath} has no jobs");

            foreach (var (name, body) in jobs)
            {
                var timeout = body.Select(line => JobTimeout().Match(line)).FirstOrDefault(match => match.Success);
                timeout.ShouldNotBeNull($"{workflow.RelativePath}: job '{name}' has no timeout-minutes");
                int.Parse(timeout.Groups["minutes"].Value, System.Globalization.CultureInfo.InvariantCulture)
                    .ShouldBeInRange(1, MaxJobMinutes, $"{workflow.RelativePath}: job '{name}'");
            }
        }
    }

    [Fact]
    public void Workflows_default_to_read_only_permissions()
    {
        foreach (var workflow in WorkflowFile.All())
        {
            var permissions = workflow.TopLevelBlock("permissions");

            permissions.ShouldContain(line => ContentsRead().IsMatch(line), $"{workflow.RelativePath} must declare contents: read at the top level");
            permissions.ShouldNotContain(line => line.Contains("write", StringComparison.Ordinal), $"{workflow.RelativePath} grants write permissions at the top level");
        }
    }

    [Fact]
    public void CodeQl_and_scorecard_are_configured()
    {
        var codeQl = WorkflowFile.Load(Repository.PathOf(".github/workflows/codeql.yml")).Text;
        codeQl.ShouldContain("github/codeql-action/init@");
        codeQl.ShouldContain("csharp");
        codeQl.ShouldContain("language: actions");
        codeQl.ShouldContain("security-extended");
        codeQl.ShouldContain("pull_request:");
        codeQl.ShouldContain("push:");
        codeQl.ShouldContain("schedule:");

        var scorecard = WorkflowFile.Load(Repository.PathOf(".github/workflows/scorecard.yml")).Text;
        scorecard.ShouldContain("ossf/scorecard-action@");
        scorecard.ShouldContain("publish_results: true");
        scorecard.ShouldContain("push:");
        scorecard.ShouldContain("schedule:");
    }

    [Fact]
    public void Contract_samples_check_reacts_to_labels()
    {
        var workflow = WorkflowFile.Load(Repository.PathOf(".github/workflows/contract-samples.yml")).Text;

        workflow.ShouldContain("pull_request:");
        workflow.ShouldContain("labeled");
        workflow.ShouldContain("unlabeled");
        workflow.ShouldContain("no-sample-impact");
    }

    [Fact]
    public void Pull_request_checks_react_to_edits()
    {
        var workflow = WorkflowFile.Load(Repository.PathOf(".github/workflows/pull-request.yml"));

        workflow.Text.ShouldContain("edited");
        workflow.Jobs().Keys.ShouldBe(["pr-title", "internal-references"], ignoreOrder: true);
    }

    [Fact]
    public void Dependabot_covers_nuget_and_actions_weekly()
    {
        var dependabot = Repository.ReadText(".github/dependabot.yml");

        dependabot.ShouldContain("package-ecosystem: nuget");
        dependabot.ShouldContain("package-ecosystem: github-actions");
        Regex.Count(dependabot, @"interval:\s*weekly").ShouldBe(2);
        Regex.Count(dependabot, @"day:\s*monday").ShouldBe(2);
        dependabot.ShouldContain("groups:");
    }

    [GeneratedRegex(@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[0-9a-f]{40}$")]
    private static partial Regex PinnedAction();

    [GeneratedRegex(@"^\s{4}timeout-minutes:\s*(?<minutes>\d+)\s*$")]
    private static partial Regex JobTimeout();

    [GeneratedRegex(@"^\s*contents:\s*read\s*$")]
    private static partial Regex ContentsRead();
}
