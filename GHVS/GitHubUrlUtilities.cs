using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitHub.Models;
using GitHub.Primitives;
using LibGit2Sharp;
using Octokit.GraphQL;

namespace GHVS
{
    public class GitHubUrlUtilities
    {
        static readonly Regex urlCommentRegex = new Regex($"pull/(?<pull>[0-9]+)([^#]*)#(discussion_r|r)(?<databaseId>[0-9]+)", RegexOptions.Compiled);
        static readonly Regex urlDiffRegex = new Regex($"pull/(?<pull>[0-9]+)([^#]*)#diff-(?<md5>[0-9a-f]+)R(?<line>[0-9]+)(-R(?<lineTo>[0-9]+))?", RegexOptions.Compiled);

        public static async Task<string> CommentToBlobUrl(Func<string, IConnection> connectionFactory, string commentUrl)
        {
            var uri = new UriString(commentUrl);
            var commentInfo = FindCommentInfo(commentUrl);
            if (commentInfo == default)
            {
                return null;
            }

            var connection = connectionFactory(commentUrl);

            var query = new Query()
                .Repository(owner: uri.Owner, name: uri.RepositoryName)
                .PullRequest(number: commentInfo.Pull)
                .Reviews(first: 100)
                .Nodes
                .Select(r => r.Comments(100, null, null, null)
                .Nodes
                .Select(c => new
                {
                    c.DatabaseId,
                    c.Path,
                    c.DiffHunk,
                    Commit = c.Commit.Oid
                })
                .ToList())
                .Compile();

            var result = await connection.Run(query);

            foreach (var review in result)
            {
                foreach (var comment in review.Where(c => c.DatabaseId == commentInfo.DatabaseId))
                {
                    foreach (var chunk in DiffUtilities.ParseFragment(comment.DiffHunk))
                    {
                        var line = chunk.Lines.Where(l => l.NewLineNumber != -1).LastOrDefault();
                        var lineNumber = line != null ? $"#L{line.NewLineNumber}" : "";
                        return $"{uri.ToRepositoryUrl()}/blob/{comment.Commit}/{comment.Path}{lineNumber}";
                    }
                }
            }

            return null;
        }


        public static async Task<string> DiffToBlobUrl(Func<string, IConnection> connectionFactory, string workingDir, string diffUrl)
        {
            var uri = new UriString(diffUrl);
            var diffInfo = FindDiffInfo(diffUrl);
            if (diffInfo == default)
            {
                return null;
            }

            var connection = connectionFactory(diffUrl);

            var query = new Query()
                .Repository(owner: uri.Owner, name: uri.RepositoryName)
                .PullRequest(number: diffInfo.Pull)
                .Select(pr => new
                {
                    pr.HeadRefOid
                })
                .Compile();

            var result = await connection.Run(query);

            string path = ResolvePath(workingDir, diffInfo.PathMd5);
            if (path == null)
            {
                throw new ArgumentException("Could't resolve path with MD5 {diffInfo.PathMd5}");
            }

            var line = diffInfo.Line != 0 ? $"#L{diffInfo.Line}" : "";
            if (diffInfo.LineTo != 0)
            {
                line += $"-L{diffInfo.LineTo}";
            }

            return $"{uri.ToRepositoryUrl()}/blob/{result.HeadRefOid}/{path}{line}";
        }

        public static string ToMd5(string text)
        {
            var enc = Encoding.GetEncoding(0);
            byte[] buffer = enc.GetBytes(text);
            var md5 = MD5.Create();
            var hash = BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "").ToLowerInvariant();
            return hash;
        }

        static string ResolvePath(string workingDir, string pathMd5)
        {
            using (var repository = new Repository(workingDir))
            {
                return ResolvePath(repository.Head.Tip.Tree, pathMd5);
            }
        }

        static string ResolvePath(Tree tree, string pathMd5)
        {
            foreach (var entry in tree)
            {
                switch (entry.TargetType)
                {
                    case TreeEntryTargetType.Blob:
                        if (ToMd5(entry.Path) == pathMd5)
                        {
                            return entry.Path;
                        }
                        break;
                    case TreeEntryTargetType.Tree:
                        if (ResolvePath(entry.Target as Tree, pathMd5) is string path)
                        {
                            return path;
                        }
                        break;

                }
            }

            return null;
        }

        static (int Pull, int DatabaseId) FindCommentInfo(string url)
        {
            var uri = new UriString(url);

            var repositoryUrl = uri.ToRepositoryUrl().ToString();
            var repositoryPrefix = repositoryUrl + "/";
            if (!url.StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            var subpath = url.Substring(repositoryPrefix.Length);

            var match = urlCommentRegex.Match(subpath);
            if (!match.Success)
            {
                return default;
            }

            var pull = int.Parse(match.Groups["pull"].Value);
            var databaseId = int.Parse(match.Groups["databaseId"].Value);
            return (pull, databaseId);
        }

        public static (int Pull, string PathMd5, int Line, int LineTo) FindDiffInfo(string url)
        {
            var uri = new UriString(url);

            var repositoryUrl = uri.ToRepositoryUrl().ToString();
            var repositoryPrefix = repositoryUrl + "/";
            if (!url.StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            var subpath = url.Substring(repositoryPrefix.Length);

            var match = urlDiffRegex.Match(subpath);
            if (!match.Success)
            {
                return default;
            }

            var pull = int.Parse(match.Groups["pull"].Value);
            var sha = match.Groups["md5"].Value;
            var line = int.Parse(match.Groups["line"].Value);
            var lineToValue = match.Groups["lineTo"].Value;
            var lineTo = !string.IsNullOrEmpty(lineToValue) ? int.Parse(lineToValue) : 0;
            return (pull, sha, line, lineTo);
        }
    }
}
