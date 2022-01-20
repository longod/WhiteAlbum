namespace WA
{
    using System.Windows.Media;

    internal interface IIntermediateResult
    {
    }

    // 特定の画像フォーマットによらない中間画像情報
    // レンダラーを差し替えられるように考慮しているためであり、
    // 差し替えた場合そのレンダラーが要求するフォーマットに変換できるようにする
    // グルーコードが増えてしまうが、レンダラーとデコーダーを分離可能にするため
    internal class ImageIntermediateResult : IIntermediateResult
    {
        internal ImageInfo Info;

        internal Color[] Palette { get; set; }

        internal byte[] Binary { get; set; }
    }

    // temp
    internal class ArchiveIntermediateResult : IIntermediateResult
    {
        // fileinfo
        // or extracted binary 別の型のほうがいいかもしれない
        internal PackedFile[] files;
    }

    internal class BinaryIntermediateResult : IIntermediateResult
    {
        internal byte[] Binary { get; set; }
    }
}
