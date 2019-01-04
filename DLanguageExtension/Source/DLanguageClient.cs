using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace DLanguageExtension
{
    public sealed class DContentDefinition
    {
        [Export]
        [Name("d")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition DContentTypeDefinition;

        [Export]
        [FileExtension(".d")]
        [ContentType("d")]
        internal static FileExtensionToContentTypeDefinition DFileExtensionDefinition;

        [Export]
        [FileExtension(".di")]
        [ContentType("d")]
        internal static FileExtensionToContentTypeDefinition DiFileExtensionDefinition;
    }

    [ContentType("d")]
    [Export(typeof(ILanguageClient))]
    public sealed class DLanguageClient : ILanguageClient
    {
        public string Name => "DLS";
        public object InitializationOptions => new object();
        public IEnumerable<string> ConfigurationSections => new string[] { "d.dls" };
        public IEnumerable<string> FilesToWatch => new string[] { "dub.json", "dub.sdl", "dub.selections.json", ".gitmodules", "*.ini" };

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        private uint progressCookie = 0;

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var dlsPath = Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA"), "dub", "packages", ".bin", "dls-latest", "dls.exe");

            var info = new ProcessStartInfo()
            {
                FileName = "dub",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!File.Exists(dlsPath))
            {
                await WithStatusbarAsync(sb => sb.SetText("Removing any previous DLS version"));
                info.Arguments = "remove dls";
                var removeProcess = new Process() { StartInfo = info };
                removeProcess.Start();
                await removeProcess.WaitForExitAsync();

                await WithStatusbarAsync(sb => sb.SetText("Fetching DLS"));
                info.Arguments = "fetch dls";
                var fetchProcess = new Process() { StartInfo = info };
                fetchProcess.Start();
                await fetchProcess.WaitForExitAsync();

                await WithStatusbarAsync(sb => sb.SetText("Installing DLS"));
                info.Arguments = "run --quiet dls:bootstrap -- --progress";
                var bootstrapProcess = new Process() { StartInfo = info };
                bootstrapProcess.Start();
                await ReportInstallProgressAsync(bootstrapProcess.StandardError);

                dlsPath = await bootstrapProcess.StandardOutput.ReadToEndAsync();
            }

            info.FileName = dlsPath.Trim();
            info.Arguments = "";
            var dlsProcess = new Process() { StartInfo = info };

            if (dlsProcess.Start())
            {
                return new Connection(dlsProcess.StandardOutput.BaseStream, dlsProcess.StandardInput.BaseStream);
            }

            return null;
        }

        public async Task OnLoadedAsync() => await StartAsync.InvokeAsync(this, EventArgs.Empty);

        public Task OnServerInitializedAsync() => Task.CompletedTask;

        public Task OnServerInitializeFailedAsync(Exception e) => Task.CompletedTask;

        private async Task ReportInstallProgressAsync(StreamReader error)
        {
            var firstLine = await error.ReadLineAsync();
            var total = Convert.ToUInt32(firstLine);

            while (!error.EndOfStream)
            {
                var line = await error.ReadLineAsync();

                switch (line)
                {
                    case "extract":
                        await WithStatusbarAsync(sb => sb.Progress(ref progressCookie, 0, "Extracting", 0, 0));
                        break;

                    default:
                        await WithStatusbarAsync(sb => sb.Progress(ref progressCookie, 1, "Installing DLS", Convert.ToUInt32(line), total));
                        break;
                }
            }

            await WithStatusbarAsync(sb => sb.Clear());
        }

        private async Task WithStatusbarAsync(Action<IVsStatusbar> action)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var statusbar = await ServiceProvider.GetGlobalServiceAsync<SVsStatusbar, IVsStatusbar>();
            action(statusbar);
        }
    }
}
