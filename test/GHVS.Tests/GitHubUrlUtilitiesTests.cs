using GHVS;
using NUnit.Framework;

public static class GitHubUrlUtilitiesTests
{
    public class TheFindDiffInfoMethod
    {
        [TestCase("https://github.com", 0, null, 0, 0)]
        [TestCase("https://github.com/jcansdale/ghvs/pull/3/files", 0, null, 0, 0)]
        [TestCase("https://github.com/jcansdale/ghvs/pull/3/files#diff-94347eb962364530d2993fdefc6da571R7", 3, "94347eb962364530d2993fdefc6da571", 7, 0)]
        [TestCase("https://github.com/jcansdale/ghvs/pull/3/files#diff-94347eb962364530d2993fdefc6da571R7-R8", 3, "94347eb962364530d2993fdefc6da571", 7, 8)]
        public void FindDiffInfo(string url, int expectPull, string expectSha, int expectLine, int expectLineTo)
        {
            var result = GitHubUrlUtilities.FindDiffInfo(url);

            Assert.That(result, Is.EqualTo((expectPull, expectSha, expectLine, expectLineTo)));
        }
    }

    public class TheFindCommentInfoMethod
    {
        [TestCase("https://github.com/jcansdale/ghvs/pull/3", 0, 0)]
        [TestCase("https://github.com/jcansdale/ghvs/pull/3#discussion_r342457357", 3, 342457357)]
        public void FindCommentInfo(string url, int expectPull, int expectDatabaseId)
        {
            var result = GitHubUrlUtilities.FindCommentInfo(url);

            Assert.That(result, Is.EqualTo((expectPull, expectDatabaseId)));
        }
    }

    public class TheToMd5Method
    {
        [TestCase("GHVS.sln", "aab415af81102bd330b705fb25c2a199")]
        public void ToMd5(string str, string expectMd5)
        {
            var result = GitHubUrlUtilities.ToMd5(str);

            Assert.That(result, Is.EqualTo(expectMd5));
        }
    }
}
