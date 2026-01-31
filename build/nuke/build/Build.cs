using System;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using UnifyBuild.Nuke;

using static Nuke.Common.IO.FileSystemTasks;

class Build : UnifyBuildBase
{
    [GitVersion] readonly GitVersion GitVersion;

    public Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            if (Context.Solution is null)
                throw new InvalidOperationException("BuildContext.Solution is not configured; cannot run tests.");

            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(Context.Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild());
        });

    public Target TestVelocity => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = (Context.RepoRoot / "project/tests/Geosphere.Plate.Velocity.Tests/Geosphere.Plate.Velocity.Tests.csproj").ToString();
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .EnableNoBuild());
        });

    void SyncLatestArtifactsImpl()
    {
        var artifactsRoot = Context.RepoRoot / "build/_artifacts";
        var sourceVersion = Context.ArtifactsVersion ?? Context.Version ?? "local";
        var source = artifactsRoot / sourceVersion;
        var latest = artifactsRoot / "latest";

        if (!Directory.Exists(source))
            throw new InvalidOperationException($"Artifacts source '{source}' does not exist. Run publish/pack targets first.");

        EnsureCleanDirectory(latest);
        CopyDirectoryRecursively(source, latest, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite);
    }

    public Target PackProjectsLatest => _ => _
        .DependsOn(PackProjects)
        .Executes(SyncLatestArtifactsImpl);

    public Target SyncLatestArtifactsProjects => _ => _
        .Executes(SyncLatestArtifactsImpl);

    protected override DotNetPackSettings ApplyCommonPackSettings(
        DotNetPackSettings settings,
        string project,
        AbsolutePath outputDir)
    {
        settings = base.ApplyCommonPackSettings(settings, project, outputDir);

        // Use the Context.NuGetOutputDir which is already versioned
        return settings.SetOutputDirectory(Context.NuGetOutputDir);
    }

    protected override BuildContext Context
    {
        get
        {
            if (GitVersion != null)
            {
                Environment.SetEnvironmentVariable("GITVERSION_MAJORMINORPATCH", GitVersion.MajorMinorPatch);
                Environment.SetEnvironmentVariable("Version", GitVersion.FullSemVer);
            }
            return BuildContextLoader.FromJson(RootDirectory, "build/build.config.json");
        }
    }

    public static int Main() => Execute<Build>(x => x.Compile);
}
