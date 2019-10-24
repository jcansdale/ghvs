using GitHub.Primitives;
using System;
using System.Web;

namespace GHVS
{
    public static class XGitHubClientUtilities
    {
        const string UriPrefix = "x-github-client://openRepo/";

        public static string FindGitHubUrl(string uri)
        {
            if (!uri.StartsWith(UriPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            uri = uri.Substring(UriPrefix.Length);

            var values = HttpUtility.ParseQueryString(new Uri(uri).Query);
            var branch =  values["branch"];
            var filepath = values["filepath"];

            if (branch != null)
            {
                if(filepath is null)
                {
                    var repositoryUrl = new UriString(uri).ToRepositoryUrl();
                    return $"{repositoryUrl}/tree/{branch}";
                }
                else
                {
                    var repositoryUrl = new UriString(uri).ToRepositoryUrl();
                    return $"{repositoryUrl}/blob/{branch}/{filepath}";
                }
            }

            return uri;
        }

        static void go()
        {
            var uri = new Uri("x-github-client://openRepo/https://github.com/github/VisualStudio?branch=master&filepath=README.md");

            var values = HttpUtility.ParseQueryString(uri.Query);
            Console.WriteLine(values["branch"]);
            Console.WriteLine(values["filepath"]);
        }
    }
}
