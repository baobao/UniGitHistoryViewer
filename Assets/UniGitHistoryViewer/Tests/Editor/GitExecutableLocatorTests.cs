using System.IO;
using NUnit.Framework;

namespace info.shibuya24.Tests
{
    public sealed class GitExecutableLocatorTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "UniGitHistoryViewerTests " + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "git"), string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [Test]
        public void T1_PATHの中からgitを見つける()
        {
            string pathVariable = string.Join(Path.PathSeparator.ToString(), "/nonexistent-dir", _directory);

            Assert.That(GitExecutableLocator.FindInPath(pathVariable, "git"), Is.EqualTo(Path.Combine(_directory, "git")));
        }

        [Test]
        public void T2_相対パスのPATHは無視する()
        {
            string pathVariable = string.Join(Path.PathSeparator.ToString(), ".", "relative/bin");

            Assert.That(GitExecutableLocator.FindInPath(pathVariable, "git"), Is.Null);
        }

        [Test]
        public void T3_ダブルクォートで囲まれたPATHも扱える()
        {
            Assert.That(GitExecutableLocator.FindInPath($"\"{_directory}\"", "git"), Is.EqualTo(Path.Combine(_directory, "git")));
        }

        [Test]
        public void T4_PATHが空ならnull()
        {
            Assert.That(GitExecutableLocator.FindInPath(string.Empty, "git"), Is.Null);
        }
    }
}
