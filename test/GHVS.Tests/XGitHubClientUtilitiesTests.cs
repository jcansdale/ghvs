using GHVS;
using NUnit.Framework;

public static class XGitHubClientUtilitiesTests
{
    public class TheToGitHubUrlMethod
    {
        [TestCase("https://github.com/github/VisualStudio", null, Description = "Not an x-github-client URI")]
        [TestCase("x-github-client://openRepo/https://github.com/jcansdale/ghvs", "https://github.com/jcansdale/ghvs", Description = "A clone URI")]
        [TestCase("X-GITHUB-CLIENT://OPENREPO/https://github.com/jcansdale/ghvs", "https://github.com/jcansdale/ghvs", Description = "Upper case")]
        [TestCase("x-github-client://openRepo/https://github.com/jcansdale/ghvs?branch=master&filepath=README.md", "https://github.com/jcansdale/ghvs/blob/master/README.md", Description = "A blob URI")]
        [TestCase("x-github-client://openRepo/https://github.com/jcansdale/ghvs?branch=master&filepath=src%2FCode.cs", "https://github.com/jcansdale/ghvs/blob/master/src/Code.cs", Description = "URI encoding")]
        [TestCase("x-github-client://openRepo/https://github.com/jcansdale/ghvs?branch=prbranch", "https://github.com/jcansdale/ghvs/tree/prbranch", Description = "Open a PR")]
        public void ToGitHubUrl(string uriString, string expectUrl)
        {
            var url = XGitHubClientUtilities.FindGitHubUrl(uriString);

            Assert.That(url?.ToString(), Is.EqualTo(expectUrl));
        }

        [TestCase("https://github.com", "https://github.com")]
        [TestCase("https://branchname.review-lab.github.com", "https://github.com")]
        [TestCase("https://branch-name.review-lab.github.com", "https://github.com")]
        public void IgnoreReviewLab(string url, string expectUrl)
        {
            var result = XGitHubClientUtilities.IgnoreReviewLab(url);

            Assert.That(result, Is.EqualTo(expectUrl));
        }
    }
}
