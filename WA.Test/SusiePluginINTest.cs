using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace WA.Test
{
    public class SusiePluginINTest : IDisposable
    {
        private Susie.SusiePlugin _plugin;

        public SusiePluginINTest()
        {
#if DEBUG
            string path = @"..\..\..\..\..\Bin\spi\Win32\Debug\ifnull.spi";
#else
            string path = @"..\..\..\..\..\Bin\spi\Win32\Release\ifnull.spi";
#endif
            _plugin = new Susie.SusiePlugin(path, Susie.StringConverter.SJIS);
        }

        public void Dispose()
        {
            _plugin?.Dispose();
        }

        [Fact]
        public void TestPluginVersion()
        {
            Assert.Equal(12, _plugin.Version);
            Assert.Equal(Susie.SusiePlugin.PluginType.ImportFilter, _plugin.Type);
            Assert.Equal(Susie.SusiePlugin.PluginTarget.Normal, _plugin.Target);
        }

        [Fact]
        public void TestPluginName()
        {
            Assert.Equal("ifnull", _plugin.Name);
        }

        [Fact]
        public void TestFileFormats()
        {
            foreach (var f in _plugin.FileFormats)
            {
                Assert.Equal("*.null", f.Item1);
                Assert.Equal("null", f.Item2);
            }
        }

        [Fact]
        public void TestIsSupported()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.IsSupported("file.null", buff));
        }

        [Fact]
        public void TestIsSupportedFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.IsSupported("file.null", null));
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.Throws<ArgumentNullException>(() => _plugin.IsSupported(null, buff));
        }

        [Fact]
        public void TestConfigurationDlg()
        {
            // need hWND
            Assert.Throws<ArgumentException>(() => _plugin.ConfigurationDlg(default));
        }

        [Fact]
        public void TestGetPictureInfo()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetPictureInfo(buff, out var info));
        }

        [Fact]
        public void TestGetPictureInfoFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetPictureInfo(null, out var info));
        }

        [Fact]
        public void TestGetPicture()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetPicture(buff, out var image, out var info));
        }

        [Fact]
        public void TestGetPictureFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetPicture(null, out var image, out var info));
        }

        [Fact]
        public void TestGetPreview()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetPreview(buff, out var image, out var info));
        }

        [Fact]
        public void TestGetPreviewFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetPreview(null, out var image, out var info));
        }

        [Fact]
        public void TestGetArchiveInfo()
        {
            Assert.Throws<InvalidOperationException>(() => _plugin.GetArchiveInfo(null, out var infos));
        }

        [Fact]
        public void TestGetFileInfo()
        {
            Assert.Throws<InvalidOperationException>(() => _plugin.GetFileInfo(null, null, out var info));
        }

        [Fact]
        public void TestGetFile()
        {
            Assert.Throws<InvalidOperationException>(() => _plugin.GetFile(null, default, out var file));
        }

    }
}
