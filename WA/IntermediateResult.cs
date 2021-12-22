namespace WA
{
    using System.Windows.Media;

    internal interface IIntermediateResult
    {
    }

    // 特定の画像フォーマットによらない中間画像情報
    // レンダラーを差し替えられるように考慮しているためであり、
    // 差し替えた場合そのレンダラーが要求するフォーマットに変換できるようにする
    internal class ImageIntermediateResult : IIntermediateResult
    {
        internal ImageInfo Info;

        internal Color[] Palette { get; set; }

        internal byte[] Binary { get; set; }
    }

    // temp
    internal class ArchiveIntermediateResult : IIntermediateResult
    {
    }
}
