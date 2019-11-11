using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.Extensions;
using GitHub.Primitives;
using LibGit2Sharp;

namespace GitHub.Services
{
    public static class GitHubContextUtilities
    {
        static readonly Regex urlLineRegex = new Regex($"#L(?<line>[0-9]+)(-L(?<lineEnd>[0-9]+))?$", RegexOptions.Compiled);
        static readonly Regex urlBlobRegex = new Regex($"blob/(?<treeish>[^/]+(/[^/]+)*)/(?<blobName>[^/#]+)", RegexOptions.Compiled);

        /// <inheritdoc/>
        public static GitHubContext FindContextFromUrl(string url)
        {
            var uri = new UriString(url);
            if (!uri.IsValidUri)
            {
                return null;
            }

            if (!uri.IsHypertextTransferProtocol)
            {
                return null;
            }

            var context = new GitHubContext
            {
                Host = uri.Host,
                Owner = uri.Owner,
                RepositoryName = uri.RepositoryName,
                Url = uri
            };

            if (uri.Owner == null)
            {
                context.LinkType = LinkType.Unknown;
                return context;
            }

            if (uri.RepositoryName == null)
            {
                context.LinkType = LinkType.Unknown;
                return context;
            }

            var repositoryUrl = uri.ToRepositoryUrl().ToString();
            if (string.Equals(url, repositoryUrl, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(url, repositoryUrl + ".git", StringComparison.OrdinalIgnoreCase))
            {
                context.LinkType = LinkType.Repository;
                return context;
            }

            var repositoryPrefix = repositoryUrl + "/";
            if (!url.StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return context;
            }

            var subpath = url.Substring(repositoryPrefix.Length);

            (context.Line, context.LineEnd) = FindLine(subpath);

            context.PullRequest = FindPullRequest(url);

            var match = urlBlobRegex.Match(subpath);
            if (match.Success)
            {
                context.TreeishPath = match.Groups["treeish"].Value;
                context.BlobName = match.Groups["blobName"].Value;
                context.LinkType = LinkType.Blob;
                return context;
            }

            return context;
        }

        /// <inheritdoc/>
        public static Uri ToRepositoryUrl(GitHubContext context)
        {
            var builder = new UriBuilder("https", context.Host ?? "github.com");
            builder.Path = $"{context.Owner}/{context.RepositoryName}";
            return builder.Uri;
        }

        /// <inheritdoc/>
        public static (string commitish, string path, string commitSha) ResolveBlob(string repositoryDir, GitHubContext context, string remoteName = "origin")
        {
            Guard.ArgumentNotNull(repositoryDir, nameof(repositoryDir));
            Guard.ArgumentNotNull(context, nameof(context));

            using (var repository = new Repository(repositoryDir))
            {
                if (context.TreeishPath == null)
                {
                    // Blobs without a TreeishPath aren't currently supported
                    return (null, null, null);
                }

                if (context.BlobName == null)
                {
                    // Not a blob
                    return (null, null, null);
                }

                var objectishPath = $"{context.TreeishPath}/{context.BlobName}";
                var objectish = ToObjectish(objectishPath);
                var (commitSha, pathSha) = objectish.First();
                if (ObjectId.TryParse(commitSha, out ObjectId objectId) && repository.Lookup(objectId) != null)
                {
                    if (repository.Lookup($"{commitSha}:{pathSha}") != null)
                    {
                        return (commitSha, pathSha, commitSha);
                    }
                }

                foreach (var (commitish, path) in objectish)
                {
                    var resolveRefs = new[] { $"refs/remotes/{remoteName}/{commitish}", $"refs/tags/{commitish}" };
                    foreach (var resolveRef in resolveRefs)
                    {
                        var commit = repository.Lookup(resolveRef);
                        if (commit != null)
                        {
                            var blob = repository.Lookup($"{resolveRef}:{path}");
                            if (blob != null)
                            {
                                return (resolveRef, path, commit.Sha);
                            }

                            // Resolved commitish but not path
                            return (resolveRef, null, commit.Sha);
                        }
                    }
                }

                return (null, null, null);
            }

            IEnumerable<(string commitish, string path)> ToObjectish(string treeishPath)
            {
                var index = 0;
                while ((index = treeishPath.IndexOf('/', index + 1)) != -1)
                {
                    var commitish = treeishPath.Substring(0, index);
                    var path = treeishPath.Substring(index + 1);
                    yield return (commitish, path);
                }
            }
        }

        static (int? lineStart, int? lineEnd) FindLine(UriString gitHubUrl)
        {
            var url = gitHubUrl.ToString();

            var match = urlLineRegex.Match(url);
            if (match.Success)
            {
                int.TryParse(match.Groups["line"].Value, out int line);

                var lineEndGroup = match.Groups["lineEnd"];
                if (string.IsNullOrEmpty(lineEndGroup.Value))
                {
                    return (line, null);
                }

                int.TryParse(lineEndGroup.Value, out int lineEnd);
                return (line, lineEnd);
            }

            return (null, null);
        }

        static int? FindPullRequest(UriString gitHubUrl)
        {
            var pullRequest = FindSubPath(gitHubUrl, "/pull/")?.Split('/').First();
            if (pullRequest == null)
            {
                return null;
            }

            if (!int.TryParse(pullRequest, out int number))
            {
                return null;
            }

            return number;
        }

        static string FindSubPath(UriString gitHubUrl, string matchPath)
        {
            var url = gitHubUrl.ToString();
            var prefix = gitHubUrl.ToRepositoryUrl() + matchPath;
            if (!url.StartsWith(prefix))
            {
                return null;
            }

            var endIndex = url.IndexOf('#');
            if (endIndex == -1)
            {
                endIndex = gitHubUrl.Length;
            }

            var path = url.Substring(prefix.Length, endIndex - prefix.Length);
            return path;
        }
    }
}
