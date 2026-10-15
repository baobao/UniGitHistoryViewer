using System.Text;
using NUnit.Framework;

namespace info.shibuya24.Tests
{
    public sealed class GitHistoryTargetTests
    {
        private const string Root = "/Projects/Game";

        [Test]
        public void T1_ルート配下の相対パスを返す()
        {
            Assert.That(GitHistoryTarget.GetRelativePath("/Projects/Game/Assets/A.cs", Root), Is.EqualTo("Assets/A.cs"));
        }

        [Test]
        public void T2_ルート外ならnull()
        {
            Assert.That(GitHistoryTarget.GetRelativePath("/Projects/GameOther/Assets/A.cs", Root), Is.Null);
        }

        [Test]
        public void T3_NFDとNFCが混ざっても同じパスとして扱いNFCで返す()
        {
            string nfc = "/Projects/Game/Assets/日本語 フォルダ/メモ.txt".Normalize(NormalizationForm.FormC);
            string nfdRoot = "/Projects/Game/Assets/日本語 フォルダ".Normalize(NormalizationForm.FormD);

            Assert.That(GitHistoryTarget.GetRelativePath(nfc, nfdRoot), Is.EqualTo("メモ.txt"));
            Assert.That(GitHistoryTarget.GetRelativePath(nfc.Normalize(NormalizationForm.FormD), Root),
                Is.EqualTo("Assets/日本語 フォルダ/メモ.txt".Normalize(NormalizationForm.FormC)));
        }
    }
}
