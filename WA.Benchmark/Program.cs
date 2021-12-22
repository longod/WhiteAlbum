using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace WA.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfig config = null;

#if DEBUG
            // on debugger
            config = new DebugInProcessConfig();
#else
            // FIXME Doesnt work
            // https://github.com/dotnet/BenchmarkDotNet/issues/856#issuecomment-715910912
            // これが動作すればAnyCPUで両方試せるはずなのだが
            var x86Core31 = Job.Default
                .WithPlatform(Platform.X86)
                .WithToolchain(CsProjCoreToolchain.From(
                    NetCoreAppSettings.NetCoreApp31
                    .WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe")
                ))
                .WithId("x86 .NET Core 3.1");

            var x64Core31 = Job.Default
                .WithPlatform(Platform.X64)
                .WithToolchain(CsProjCoreToolchain.From(
                    NetCoreAppSettings.NetCoreApp31
                    .WithCustomDotNetCliPath(@"C:\Program Files\dotnet\dotnet.exe")
                ))
                .WithId("x64 .NET Core 3.1");

            // DebugInProcessConfig に近い構成
            // Platform指定で動作する
            var inProcess = Job.Default.WithToolchain(InProcessEmitToolchain.Instance).WithId("InProcess");

            config = DefaultConfig.Instance.AddJob(inProcess);
#endif

            BenchmarkRunner.Run<StringConvertion>(config);
            BenchmarkRunner.Run<MemoryCopying>(config);
        }
    }
}
