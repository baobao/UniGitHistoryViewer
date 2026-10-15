using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace info.shibuya24.Tests
{
    public sealed class GitLogParserTests
    {
        private const char Field = '\u001f';
        private const char Record = '\u001e';

        private static string Commit(string hash, string parents, string subject, string body)
        {
            return string.Join(Field.ToString(), new[]
            {
                hash, hash.Substring(0, 7), parents, "baobao", "baobao@example.com",
                "2020-07-20T13:10:36+09:00", subject, body,
            }) + Record;
        }

        [Test]
        public void T1_複数コミットを順番どおりに分解できる()
        {
            string output = Commit("aaaaaaa1", "bbbbbbb2", "Second", string.Empty) + "\n" +
                            Commit("bbbbbbb2", string.Empty, "First", string.Empty);

            List<GitCommit> commits = GitLogParser.ParseLog(output);

            Assert.That(commits.Count, Is.EqualTo(2));
            Assert.That(commits[0].Subject, Is.EqualTo("Second"));
            Assert.That(commits[0].ParentHashes, Is.EqualTo(new[] {"bbbbbbb2"}));
            Assert.That(commits[1].ParentHashes, Is.Empty);
            Assert.That(commits[0].AuthorDate, Is.EqualTo(new DateTimeOffset(2020, 7, 20, 13, 10, 36, TimeSpan.FromHours(9))));
        }

        [Test]
        public void T2_本文に空行やcommitという単語があっても壊れない()
        {
            string body = "commit message body\n\ncommit abc\n";

            List<GitCommit> commits = GitLogParser.ParseLog(Commit("aaaaaaa1", string.Empty, "日本語の件名", body));

            Assert.That(commits.Count, Is.EqualTo(1));
            Assert.That(commits[0].Subject, Is.EqualTo("日本語の件名"));
            Assert.That(commits[0].Body, Is.EqualTo("commit message body\n\ncommit abc"));
        }

        [Test]
        public void T3_マージコミットは親が2つになる()
        {
            List<GitCommit> commits = GitLogParser.ParseLog(Commit("aaaaaaa1", "bbbbbbb2 ccccccc3", "Merge", string.Empty));

            Assert.That(commits[0].IsMerge, Is.True);
        }

        [Test]
        public void T4_空の出力は0件()
        {
            Assert.That(GitLogParser.ParseLog(string.Empty), Is.Empty);
        }

        [Test]
        public void T5_変更ファイルのリネームは旧パスと新パスを持つ()
        {
            string output = "M\0Assets/A.cs\0R100\0Assets/Old.cs\0Assets/New.cs\0D\0Assets/B.cs\0";

            List<GitChangedFile> files = GitLogParser.ParseChangedFiles(output);

            Assert.That(files.Count, Is.EqualTo(3));
            Assert.That(files[0].Status, Is.EqualTo('M'));
            Assert.That(files[0].OldPath, Is.Null);
            Assert.That(files[1].Status, Is.EqualTo('R'));
            Assert.That(files[1].OldPath, Is.EqualTo("Assets/Old.cs"));
            Assert.That(files[1].Path, Is.EqualTo("Assets/New.cs"));
            Assert.That(files[2].Path, Is.EqualTo("Assets/B.cs"));
        }

        [Test]
        public void T6_相対時間は単位ごとに丸める()
        {
            DateTimeOffset now = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddSeconds(-30), now), Is.EqualTo("just now"));
            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddMinutes(-1), now), Is.EqualTo("1 minute ago"));
            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddHours(-5), now), Is.EqualTo("5 hours ago"));
            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddDays(-3), now), Is.EqualTo("3 days ago"));
            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddDays(-90), now), Is.EqualTo("3 months ago"));
            Assert.That(RelativeTimeFormatter.FormatRelative(now.AddDays(-800), now), Is.EqualTo("2 years ago"));
        }
    }
}
