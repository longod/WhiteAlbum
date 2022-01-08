namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Windows.Media.Imaging;

    // todo non build exporter
    // todo quality settings interface
    internal class BuiltInImageEncoder
    {
        private BitmapEncoder _encoder;

        internal BuiltInImageEncoder(string format, BitmapEncoder bitmapEncoder)
        {
            FormatName = format;
            _encoder = bitmapEncoder;
        }

        internal string FormatName { get; }

        internal void EncodeToFile(string path, BitmapSource image)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                _encoder.Frames.Add(BitmapFrame.Create(image));
                _encoder.Save(stream);
            }
        }
    }
}
