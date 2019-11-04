using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitHub.Models;
using GitHub.Primitives;
using Octokit.GraphQL;

namespace GHVS
{
    public class GitHubUrlUtilities
    {
        static readonly Regex urlCommentRegex = new Regex($"pull/(?<pull>[0-9]+)([^#]*)#(discussion_r|r)(?<databaseId>[0-9]+)", RegexOptions.Compiled);

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
    }
}
