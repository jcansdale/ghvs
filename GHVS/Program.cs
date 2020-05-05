using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using GitHub.Primitives;
using Microsoft.Alm.Authentication;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Win32;
using EnvDTE;

namespace GHVS
{
    [Command("ghvs")]
    [Subcommand(
        typeof(PullsCommand),
        typeof(IssuesCommand),
        typeof(ViewerCommand),
        typeof(OrganizationsCommand),        
        typeof(RepositoriesCommand),
        typeof(BranchCommand),
        typeof(UpstreamCommand),
        typeof(LoginCommand),
        typeof(LogoutCommand),
        typeof(OpenCommand),
        typeof(OpenUrlCommand),
        typeof(InstallCommand),
        typeof(UninstallCommand)
    )]
    public class Program : GitHubCommandBase
    {
        public static Task Main(string[] args)
        {
            // If single arg is file or dir then implicitly use open
            if (args.Length == 1 && args[0] is string path && (File.Exists(path) || Directory.Exists(path)))
            {
                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
                var editor = isWindows ? "--vs" : "--code";
                args = args.Prepend("open").Append(editor).ToArray();
            }

            return CommandLineApplication.ExecuteAsync<Program>(args);
        }

        protected override Task OnExecute(CommandLineApplication app)
        {
            // this shows help even if the --help option isn't specified
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }

    [Command(Description = "Show pull requests")]
    class PullsCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var connection = CreateConnection();

            var orderBy = new IssueOrder { Field = IssueOrderField.CreatedAt, Direction = OrderDirection.Desc };
            var states = new[] { PullRequestState.Open };
            var query = new Query()
                .Viewer
                .PullRequests(first: 100, orderBy: orderBy, states: states)
                .Nodes
                .Select(pr => new { Repository = pr.Repository.NameWithOwner, pr.Title, pr.Number, Author = pr.Author != null ? pr.Author.Login : null, pr.CreatedAt })
                .Compile();

            var result = await connection.Run(query);

            foreach (var pr in result)
            {
                Console.WriteLine(
@$"{pr.Repository} - {pr.Title}
#{pr.Number} opened on {pr.CreatedAt:D} by {pr.Author}
");
            }
        }
    }

    [Command(Description = "Show issues")]
    class IssuesCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var connection = CreateConnection();

            var orderBy = new IssueOrder { Field = IssueOrderField.CreatedAt, Direction = OrderDirection.Desc };
            var states = new[] { IssueState.Open };
            var query = new Query()
                .Viewer
                .Issues(first: 100, orderBy: orderBy, states: states)
                .Nodes
                .Select(pr => new { pr.Repository.NameWithOwner, pr.Title, pr.Number, pr.Author.Login, pr.CreatedAt })
                .Compile();

            var result = await connection.Run(query);

            foreach (var pr in result)
            {
                Console.WriteLine(
@$"{pr.NameWithOwner} - {pr.Title}
#{pr.Number} opened on {pr.CreatedAt:D} by {pr.Login}
");
            }
        }
    }

    [Command(Description = "Show viewer information")]
    class ViewerCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var connection = CreateConnection();

            var query = new Query()
                .Viewer
                .Select(v =>
                new
                {
                    v.Login,
                    v.Name
                })
                .Compile();

            var result = await connection.Run(query);

            Console.WriteLine($"You are signed in as {result.Login} ({result.Name})");
        }
    }

    [Command("orgs", Description = "Show visible organizations (requires 'read:org' scope)")]
    class OrganizationsCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var connection = CreateConnection();

            var query = new Query()
                .Viewer
                .Organizations(first: 100)
                .Nodes
                .Select(o => new
                {
                    o.Login,
                    Repositories = o.Repositories(null, null, null, null, null, null, null, null, null, null).TotalCount,
                    Teams = o.Teams(null, null, null, null, null, null, null, null, null, null, null).TotalCount,
                    Members = o.MembersWithRole(null, null, null, null).TotalCount,
                    Projects = o.Projects(null, null, null, null, null, null, null).TotalCount
                })
                .Compile();

            var result = await connection.Run(query);

            foreach (var o in result)
            {
                Console.WriteLine($"{o.Login} has {o.Repositories} repositories, {o.Members} members, {o.Teams} teams and {o.Projects} projects");
            }
        }
    }

    [Command("repos", Description = "List repositories")]
    class RepositoriesCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var connection = CreateConnection();

            var affiliations = new RepositoryAffiliation?[] { RepositoryAffiliation.Owner };

            var repositories = Owner is string owner
                ? new Query()
                    .RepositoryOwner(owner)
                    .Repositories(first: 100, affiliations: affiliations, ownerAffiliations: affiliations)
                : new Query()
                    .Viewer
                    .Repositories(first: 100, affiliations: affiliations, ownerAffiliations: affiliations);

            var query = repositories
                    .Nodes
                    .Select(r => new { r.Name, r.IsPrivate, ForkedFrom = r.Parent != null ? r.Parent.NameWithOwner : null })
                    .Compile();

            var result = await connection.Run(query);

            foreach (var r in result)
            {
                Console.WriteLine($"{r.Name}{(r.IsPrivate ? " [Private]" : "")}{(r.ForkedFrom != null ? " Forked from " + r.ForkedFrom : "")}");
            }
        }

        [Option("--owner", Description = "The owning user or organization")]
        public string Owner { get; }
    }

    [Command(Description = "Show information about the current branch")]
    class BranchCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var gitDirectory = LibGit2Sharp.Repository.Discover(".");
            using (var repository = new LibGit2Sharp.Repository(gitDirectory))
            {
                var head = repository.Head;
                var trackedBranch = head.TrackedBranch;
                if (trackedBranch is null)
                {
                    Console.WriteLine($"Current branch '{head.FriendlyName}' isn't tracking a remote.");
                    return;
                }

                var upstreamBranchCanonicalName = trackedBranch.UpstreamBranchCanonicalName;
                var branchName = ToBranchName(upstreamBranchCanonicalName);
                var remoteUrl = new UriString(repository.Network.Remotes[trackedBranch.RemoteName].Url);

                var pullRequestStates = new[] { PullRequestState.Open, PullRequestState.Closed, PullRequestState.Merged };
                var query = new Query()
                    .Repository(owner: remoteUrl.Owner, name: remoteUrl.RepositoryName)
                    .Ref(upstreamBranchCanonicalName)
                    .Select(r => new
                    {
                        Repository = r.Repository.NameWithOwner,
                        ForkedFrom = r.Repository.Parent != null ? r.Repository.Parent.NameWithOwner : null,
                        r.Target.Oid,
                        PullRequests = r.AssociatedPullRequests(100, null, null, null, null, branchName, null, null, pullRequestStates).Nodes.Select(pr => new
                        {
                            Repository = pr.Repository.NameWithOwner,
                            pr.Number,
                            pr.Title,
                            pr.Url,
                            Author = pr.Author != null ? pr.Author.Login : null,
                            pr.CreatedAt,
                            pr.AuthorAssociation,
                            pr.State,
                            pr.HeadRefName,
                            HeadRepository = pr.HeadRepository.NameWithOwner,
                            pr.BaseRefName,
                            BaseRepository = pr.BaseRef != null ? pr.BaseRef.Repository.NameWithOwner : null,
                            pr.HeadRefOid
                        }).ToList()
                    }).Compile();

                var connection = CreateConnection(remoteUrl);
                var result = await connection.Run(query);

                Console.WriteLine(result.ForkedFrom is null ? result.Repository : $"{result.Repository} forked from {result.ForkedFrom}");
                Console.WriteLine(result.Oid == trackedBranch.Tip.Sha ? "No new commits" : "There are new commits!");
                var prs = result.PullRequests
                    .Where(pr => pr.HeadRepository == pr.Repository); // Only show incoming pull requests
                if (prs.Count() == 0)
                {
                    Console.WriteLine("No associated pull requests");
                }
                else
                {
                    Console.WriteLine(@"
Associated pull requests:");
                    foreach (var pr in prs)
                    {
                        Console.WriteLine(
        @$"{pr.Repository} - {pr.Title} [{pr.State}]
#{pr.Number} opened on {pr.CreatedAt:D} by {pr.Author} ({pr.AuthorAssociation})");
                    }
                    Console.WriteLine();
                }
            }
        }

        static string ToBranchName(string canonicalName)
        {
            var prefix = "refs/heads/";
            if (canonicalName.StartsWith(prefix))
            {
                return canonicalName.Substring(prefix.Length);
            }

            return null;
        }
    }

    [Command(Description = "Show information about the upstream repository")]
    class UpstreamCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var gitDirectory = LibGit2Sharp.Repository.Discover(".");
            using (var repository = new LibGit2Sharp.Repository(gitDirectory))
            {
                var remote = repository.Network.Remotes.FirstOrDefault();
                if (remote is null)
                {
                    Console.WriteLine("This repository contains no remotes");
                    return;
                }

                var remoteUrl = new UriString(remote.Url);

                var openPullRequestState = new[] { PullRequestState.Open };
                var openIssueState = new[] { IssueState.Open };
                var query = new Query()
                    .Repository(owner: remoteUrl.Owner, name: remoteUrl.RepositoryName)
                    .Select(r => new
                    {
                        Repository = r.Select(p => new
                        {
                            p.NameWithOwner,
                            p.ViewerPermission,
                            DefaultBranchName = p.DefaultBranchRef.Name,
                            OpenPullRequests = p.PullRequests(null, null, null, null, null, null, null, null, openPullRequestState).TotalCount,
                            OpenIssues = p.Issues(null, null, null, null, null, null, null, openIssueState).TotalCount
                        }).Single(),
                        Parent = r.Parent == null ? null : r.Parent.Select(p => new
                        {
                            p.NameWithOwner,
                            p.ViewerPermission,
                            DefaultBranchName = p.DefaultBranchRef.Name,
                            OpenPullRequests = p.PullRequests(null, null, null, null, null, null, null, null, openPullRequestState).TotalCount,
                            OpenIssues = p.Issues(null, null, null, null, null, null, null, openIssueState).TotalCount
                        }).Single()
                    }).Compile();

                var connection = CreateConnection(remoteUrl);
                var result = await connection.Run(query);

                if (result.Parent != null)
                {
                    Console.WriteLine($"Upstream repository {result.Parent.NameWithOwner} has {result.Parent.OpenPullRequests} open pull requests and {result.Parent.OpenIssues} open issues");
                    Console.WriteLine($"The default branch is {result.Parent.DefaultBranchName}");
                    Console.WriteLine($"Viewer has permission to {result.Parent.ViewerPermission}");
                }
                else
                {
                    Console.WriteLine($"Upstream repository {result.Repository.NameWithOwner} has {result.Repository.OpenPullRequests} open pull requests and {result.Repository.OpenIssues} open issues");
                    Console.WriteLine($"The default branch is {result.Repository.DefaultBranchName}");
                    Console.WriteLine($"Viewer has permission to {result.Repository.ViewerPermission}");
                }
            }
        }
    }

    [Command(Description = "Login using GitHub Credential Manager ")]
    class LoginCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var host = Host ?? "https://github.com";

            CredentialManager.Fill(new Uri(host));

            await Task.Yield();
        }
    }


    [Command(Description = "Logout using GitHub Credential Manager")]
    class LogoutCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var host = Host ?? "https://github.com";

            CredentialManager.Reject(new Uri(host));

            await Task.Yield();
        }
    }

    [Command(Description = "Open a file or folder in Visual Studio")]
    class OpenCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            var fullPath = Path.GetFullPath(FileOrFolder);

            if (Code)
            {
                OpenVSCode(fullPath);
                return;
            }

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                OpenVSCode(fullPath);
                return;
            }

            if (VS)
            {
                await OpenVisualStudioAsync(fullPath);
                return;
            }

            await OpenVSCodeOrVisualStudio(fullPath);
        }

        static void OpenVSCode(string fullPath)
        {
            if (FindWorkingDirectory(fullPath) is string wd)
            {
                VSCodeUtilities.OpenFileInFolder(wd, fullPath);
            }
            else
            {
                VSCodeUtilities.OpenFileOrFolder(fullPath);
            }
        }

        static async Task OpenVisualStudioAsync(string fullPath)
        {
            var application = VisualStudioUtilities.GetApplicationPaths().Last();   // HACK: What if there are none?
            if (FindWorkingDirectory(fullPath) is string wd)
            {
                await VisualStudioUtilities.OpenFileInFolderAsync(application, wd, fullPath);
            }
            else
            {
                VisualStudioUtilities.OpenFileOrFolder(application, fullPath);
            }
        }

        async Task OpenVSCodeOrVisualStudio(string fullPath)
        {
            if (await VisualStudioUtilities.OpenAsync(fullPath))
            {
                return;
            }

            var workingDir = FindWorkingDirectory(fullPath);
            await CommndLineUtilities.OpenFileInFolderAsync(workingDir, fullPath);
        }

        static string FindWorkingDirectory(string fullPath)
        {
            var gitDir = LibGit2Sharp.Repository.Discover(fullPath);
            if (gitDir == null)
            {
                return null;
            }

            gitDir = gitDir.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            return Path.GetDirectoryName(gitDir);
        }

        [Argument(0, Description = "The path to open")]
        public string FileOrFolder { get; set; }

        [Option(Description = "Open in VSCode")]
        public bool Code { get; set; }

        [Option(Description = "Open in Visual Studio")]
        public bool VS { get; set; }
    }

    [Command(Description = "Open a GitHub URL in Visual Studio")]
    class OpenUrlCommand : GitHubCommandBase
    {
        protected override async Task OnExecute(CommandLineApplication app)
        {
            // Handle x-github-client URIs
            var url = XGitHubClientUtilities.FindGitHubUrl(Url) ?? Url;

            // Ignore review-lab part of URL
            url = XGitHubClientUtilities.IgnoreReviewLab(url);

            // Convert PR inline comments to blob URLs
            url = await GitHubUrlUtilities.CommentToBlobUrl(CreateConnection, url) ?? url;

            // Use live Visual Studio instance
            var solutionPaths = await VisualStudioUtilities.GetSolutionPaths();
            var workingDir = FindWorkingDirectoryForUrl(solutionPaths, url);
            if (workingDir != null)
            {
                // Convert diff to blob URLs
                url = await GitHubUrlUtilities.DiffToBlobUrl(CreateConnection, workingDir, url) ?? url;
                await VisualStudioUtilities.OpenFromUrlAsync(workingDir, url);
                return;
            }

            // Use live VSCode instance
            var codeFolders = VSCodeUtilities.GetFolders();
            workingDir = FindWorkingDirectoryForUrl(codeFolders, url);
            if (workingDir != null)
            {
                // Convert diff to blob URLs
                url = await GitHubUrlUtilities.DiffToBlobUrl(CreateConnection, workingDir, url) ?? url;
                VSCodeUtilities.OpenFromUrl(workingDir, url);
                return;
            }

            CommndLineUtilities.OpenFromUrl(url);
        }

        static string FindWorkingDirectoryForUrl(IEnumerable<string> paths, UriString targetUrl)
        {
            foreach (var path in paths)
            {
                var gitDir = LibGit2Sharp.Repository.Discover(path);
                if (gitDir == null)
                {
                    continue;
                }

                using (var repository = new LibGit2Sharp.Repository(gitDir))
                {
                    var remoteName = repository.Head.RemoteName;
                    if (remoteName == null)
                    {
                        continue;
                    }

                    using (var remote = repository.Network.Remotes[remoteName])
                    {
                        var remoteUrl = new UriString(remote.Url);
                        if (UriString.RepositoryUrlsAreEqual(remoteUrl, targetUrl) ||
                            // HACK: Match forks when the repository name is the same!
                            string.Equals(remoteUrl.RepositoryName, targetUrl.RepositoryName, StringComparison.OrdinalIgnoreCase))
                        {
                            var workingDir = repository.Info.WorkingDirectory;
                            workingDir = workingDir.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                            return workingDir;
                        }
                    }
                }
            }

            return null;
        }

        [Argument(0, Description = "The GitHub URL to open")]
        public string Url { get; set; }
    }

    [Command(Description = "Install 'x-github-client' protocol handler")]
    class InstallCommand : GitHubCommandBase
    {
        protected override Task OnExecute(CommandLineApplication app)
        {
            var exeFile = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var commandLine = $"\"{exeFile}\" open-url \"%1\"";

            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\x-github-client", null, "GitHub Protocol Handler");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\x-github-client", "URL Protocol", "");
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\x-github-client\shell\Open\Command", null, commandLine);

            Console.WriteLine("A protocol handler for 'x-github-client' URIs was installed.");
            Console.WriteLine();
            Console.WriteLine("Please add an 'Open in Editor' bookmarklet to your browser with the following:");
            Console.WriteLine("javascript:window.location.href='x-github-client://openRepo/'+window.location.href");
            Console.WriteLine();

            return Task.CompletedTask;
        }
    }

    [Command(Description = "Uninstall 'x-github-client' protocol handler")]
    class UninstallCommand : GitHubCommandBase
    {
        protected override Task OnExecute(CommandLineApplication app)
        {
            using (var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
            {
                registryKey.DeleteSubKey(@"x-github-client\shell\Open\Command", false);
                registryKey.DeleteSubKey(@"x-github-client\shell\Open", false);
                registryKey.DeleteSubKey(@"x-github-client\shell", false);
                registryKey.DeleteSubKey(@"x-github-client", false);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// This base type provides shared functionality.
    /// Also, declaring <see cref="HelpOptionAttribute"/> on this type means all types that inherit from it
    /// will automatically support '--help'
    /// </summary>
    [HelpOption("--help")]
    public abstract class GitHubCommandBase
    {
        protected abstract Task OnExecute(CommandLineApplication app);

        protected IConnection CreateConnection(string host = null)
        {
            host = Host ?? host ?? "https://github.com";

            var productInformation = new ProductHeaderValue("GHVS", "0.1");
            var token = GetToken(host);

            var hostAddress = HostAddress.Create(host);
            var connection = new Connection(productInformation, hostAddress.GraphQLUri, token);
            return connection;
        }

        protected string GetToken(string url)
        {
            if (AccessToken is string accessToken)
            {
                return accessToken;
            }

            var targetUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
            switch (SecretStore)
            {
                case SecretStores.Credential:
                    // Use the built in credential store
                    var userPass = CredentialManager.Fill(new Uri(targetUrl));

                    // Experimental VS Online support
                    if(userPass.Password == "x-oauth-basic")
                    {
                        return userPass.Username;
                    }

                    if (userPass.Username != null)
                    {
                        return userPass.Password;
                    }
                    break;
                default:
                    // Use a specific secret store
                    var secretStore = CreateSecretStore();
                    var auth = new BasicAuthentication(secretStore);
                    var creds = auth.GetCredentials(new TargetUri(targetUrl));
                    if (creds != null)
                    {
                        return creds.Password;
                    }
                    break;
            }

            throw new ApplicationException($"Couldn't find credentials for {url}");
        }

        SecretStore CreateSecretStore() =>  SecretStore switch
        {
            SecretStores.Git => new SecretStore("git", Secret.UriToIdentityUrl),
            SecretStores.GHfVS => new SecretStore("GitHub for Visual Studio", (tu, ns) => $"{ns} - {tu.ToString(true, true, true)}"),
            _ => throw new InvalidOperationException($"Unknown secret store {SecretStore}")
        };

        [Option("--host", Description = "The host URL")]
        public string Host { get; }

        [Option("--access-token", Description = "The access token to use")]
        public string AccessToken { get; }

        [Option("--secret-store", Description = "The secret store to use (Git or GHfVS)")]
        public SecretStores SecretStore { get; } = SecretStores.Credential;
    }

    public enum SecretStores
    {
        Credential, Git, GHfVS
    }
}
