using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace WA.Test
{
    public class SusiePluginAMTest : IDisposable
    {

        private Susie.SusiePlugin _plugin;

        public SusiePluginAMTest(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.WriteLine("current directory: " + AppContext.BaseDirectory);
#if DEBUG
            string path = @"..\..\..\..\..\Bin\spi\Win32\Debug\axnull.spi";
#else
            string path = @"..\..\..\..\..\Bin\spi\Win32\Release\axnull.spi";
#endif
            testOutputHelper.WriteLine("plugin: " + System.IO.Path.GetFullPath(path));
            _plugin = new Susie.SusiePlugin(path, Susie.StringConverter.SJIS);
        }

        public void Dispose()
        {
            _plugin?.Dispose();
        }

        [Fact]
        public void TestPluginVersion()
        {
            Assert.Equal(34, _plugin.Version);
            Assert.Equal(Susie.SusiePlugin.PluginType.ArchiveExtractor, _plugin.Type);
            Assert.Equal(Susie.SusiePlugin.PluginTarget.MultiPicture, _plugin.Target);
        }

        [Fact]
        public void TestPluginName()
        {
            Assert.Equal("axnull", _plugin.Name);
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
            Assert.Throws<InvalidOperationException>(() => _plugin.GetPictureInfo(null, out var infos));
        }

        [Fact]
        public void TestGetPicture()
        {
            Assert.Throws<InvalidOperationException>(() => _plugin.GetPicture(null, out var image, out var infos));
        }

        [Fact]
        public void TestGetPreview()
        {
            Assert.Throws<InvalidOperationException>(() => _plugin.GetPreview(null, out var image, out var infos));
        }

        [Fact]
        public void TestGetArchiveInfo()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetArchiveInfo(buff, out var infos));
        }

        [Fact]
        public void TestGetArchiveInfoFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetArchiveInfo(null, out var files));
        }

        [Fact]
        public void TestGetFileInfo()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetFileInfo(buff, "file", out var info));
        }

        [Fact]
        public void TestGetFileInfoFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetFileInfo(null, "file", out var info));
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.Throws<ArgumentNullException>(() => _plugin.GetFileInfo(buff, null, out var info));
        }

        [Fact]
        public void TestGetFile()
        {
            byte[] buff = new byte[Susie.API.Constant.MinFileSize];
            Assert.True(_plugin.GetFile(buff, default, out var file));
        }

        [Fact]
        public void TestGetFileFailure()
        {
            Assert.Throws<ArgumentNullException>(() => _plugin.GetFile(null, default, out var file));
        }

    }
}
