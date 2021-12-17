using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace WA.Test
{
    public class SusiePluginArchiveExtractorTest : IDisposable
    {
        private Susie.SusiePlugin _susie;

        public SusiePluginArchiveExtractorTest()
        {
            //Environment.CurrentDirectory = AppContext.BaseDirectory;
            // ここでこけるかどうかのテストをどうする
            // fixme dotnet testで実行した場合に失敗してしまう
#if DEBUG
            string path = @"..\..\..\..\..\Bin\spi\Win32\Debug\axnull.spi";
#else
            string path = @"..\..\..\..\..\Bin\spi\Win32\Release\axnull.spi";
#endif
            _susie = new Susie.SusiePlugin(path, Susie.StringConverter.SJIS);
        }

        public void Dispose()
        {
            _susie?.Dispose();
        }

        [Fact]
        public void TestPluginVersion()
        {
            Assert.Equal(34, _susie.Version);
            Assert.Equal(Susie.SusiePlugin.PluginType.ArchiveExtractor, _susie.Type);
            Assert.Equal(Susie.SusiePlugin.PluginTarget.MultiPicture, _susie.Target);
        }

        [Fact]
        public void TestPluginName()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestFileFormats()
        {
            Assert.True(false);
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
        public void TestGetArchiveInfo()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestGetFileInfo()
        {
            Assert.True(false);
        }

        [Fact]
        public void TestGetFile()
        {
            Assert.True(false);
        }

    }
}
