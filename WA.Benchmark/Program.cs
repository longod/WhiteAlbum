using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System;

namespace WA.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfig config = null;

            // FIXME works on x86, x64, AnyCPU
#if DEBUG
            //config = new DebugBuildConfig();
            config = new DebugInProcessConfig();
#else
            config = new DebugInProcessConfig(); // release でもデバッガ経由だと動かせる

            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
            //var config = ManualConfig.Create(DefaultConfig.Instance).AddJob(Job.Default.WithPlatform(Platform.X86));
            // doesnt work
            var x86core31 = Job.ShortRun
                .WithPlatform(BenchmarkDotNet.Environments.Platform.X86)
                .WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp31
                //.WithCustomDotNetCliPath(@"..\..\..\..\\dotnet-runtime-3.1.21-win-x86\dotnet.exe")
                .WithCustomDotNetCliPath(@"C:\Program Files (x86)\dotnet\dotnet.exe")
                ))
                .WithId("x86 .NET Core 3.1"); // displayed in the results table
            //config = DefaultConfig.Instance.AddJob(x86core31);
#endif

            BenchmarkRunner.Run<MemoryCopying>(config);
            //BenchmarkRunner.Run<BitmapCreation>(config);
        }
    }
}
