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
            //Environment.CurrentDirectory = AppContext.BaseDirectory;
            // ここでこけるかどうかのテストをどうする
            // fixme dotnet testで実行した場合に失敗してしまう
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
            Assert.True(false);
        }

        [Fact(Skip = "needs HWND")]
        public void TestConfigurationDlg()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestGetPictureInfo()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestGetPicture()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestGetPreview()
        {
            Assert.True(false);
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
