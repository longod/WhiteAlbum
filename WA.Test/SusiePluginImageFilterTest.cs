using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace WA.Test
{
    public class SusiePluginImageFilterTest : IDisposable
    {
        private Susie.SusiePlugin _susie;

        public SusiePluginImageFilterTest()
        {
            //Environment.CurrentDirectory = AppContext.BaseDirectory;
            // ここでこけるかどうかのテストをどうする
            // fixme dotnet testで実行した場合に失敗してしまう
            string path = @"..\..\..\..\..\Bin\spi\Win32\Debug\ifnull.spi";
            _susie = new Susie.SusiePlugin(path, Susie.StringConverter.SJIS);
        }

        public void Dispose()
        {
            _susie?.Dispose();
        }

        [Fact]
        public void TestVersion()
        {
            Assert.Equal(Susie.SusiePlugin.PluginType.ImportFilter, _susie.Type);
        }
    }
}
