#load "artifact.cake"
#load "changelog.cake"
#load "github.cake"

#tool nuget:?package=xunit.runner.console&version=2.3.1

#addin nuget:?package=Cake.Bumpy&version=0.8.0

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var version = "0.11.0";

RepositoryHome = "..";

var gitHubSettings = new GitHubSettings
{
    Owner = "fwinkelbauer",
    Repository = "Bumpy",
    Token = EnvironmentVariable("GITHUB_RELEASE_TOKEN"),
    GitTag = version,
    TextBody = "More information about this release can be found in the [changelog](https://github.com/fwinkelbauer/Bumpy/blob/master/CHANGELOG.md)",
    IsDraft = true,
    IsPrerelease = false
};

Task("Clean").Does(() =>
{
    CleanArtifacts();
    CleanDirectories($"Bumpy*/bin/{configuration}");
    CleanDirectories($"Bumpy*/obj/{configuration}");
});

Task("Restore").Does(() =>
{
    NuGetRestore("Bumpy.sln");
});

Task("Build").Does(() =>
{
    MSBuild("Bumpy.sln", new MSBuildSettings { Configuration = configuration, WarningsAsError = true });
    StoreBuildArtifacts("Bumpy", $"Bumpy/bin/{configuration}/**/*");
});

Task("Test").Does(() =>
{
    XUnit2($"*.Tests/bin/{configuration}/*.Tests.dll");
});

Task("CreatePackages").Does(() =>
{
    PackChocolateyArtifacts("NuSpec/Chocolatey/**/*.nuspec");
    PackNuGetArtifacts("NuSpec/NuGet/**/*.nuspec");
});

Task("PushPackages").Does(() =>
{
    BumpyEnsure();
    EnsureChangelog("../CHANGELOG.md", version);

    PublishChocolateyArtifact("Bumpy.Portable", "https://push.chocolatey.org/");
    PublishNuGetArtifact("Bumpy", "https://www.nuget.org/api/v2/package");

    var mime = "application/zip";
    PublishGitHubReleaseWithArtifacts(
        gitHubSettings,
        new GitHubAsset(GetChocolateyArtifact("Bumpy.Portable"), mime),
        new GitHubAsset(GetNuGetArtifact("Bumpy"), mime));
});

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CreatePackages");

Task("Publish")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CreatePackages")
    .IsDependentOn("PushPackages");

RunTarget(target);
