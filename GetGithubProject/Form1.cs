using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GetGithubProject
{
    public partial class Form1 : Form
    {
        private readonly TextBox repositoryTextBox;
        private readonly TextBox destinationTextBox;
        private readonly TextBox logTextBox;
        private readonly Button startButton;
        private readonly Button cancelButton;
        private readonly Button browseButton;
        private readonly Button openFolderButton;
        private readonly ProgressBar progressBar;
        private readonly Label statusLabel;
        private CancellationTokenSource cancellationSource;
        private Process activeBuildProcess;
        private string latestProjectDirectory;

        public Form1()
        {
            InitializeComponent();

            AutoScaleMode = AutoScaleMode.None;
            Text = "GitHub 项目下载器";
            ClientSize = new System.Drawing.Size(920, 640);
            MinimumSize = new System.Drawing.Size(850, 590);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = System.Drawing.Color.FromArgb(243, 246, 251);
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = System.Drawing.Color.FromArgb(24, 39, 67)
            };
            var title = new Label
            {
                Text = "GitHub 项目下载器",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 21F, System.Drawing.FontStyle.Bold),
                AutoSize = true,
                Location = new System.Drawing.Point(30, 10)
            };
            var subtitle = new Label
            {
                Text = "优先下载 GitHub Release；没有可用应用时下载源码并本机构建",
                ForeColor = System.Drawing.Color.FromArgb(205, 216, 233),
                AutoSize = true,
                Location = new System.Drawing.Point(34, 55)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BackColor,
                Padding = new Padding(24, 12, 24, 18),
                ColumnCount = 1,
                RowCount = 5
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 236F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var card = new Panel
            {
                BackColor = System.Drawing.Color.White,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(18, 10, 18, 10)
            };
            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                ColumnCount = 1,
                RowCount = 6,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            var urlLabel = MakeLabel("GitHub 仓库地址", 0, 0, true);
            urlLabel.AutoSize = true;
            urlLabel.Margin = Padding.Empty;
            urlLabel.Anchor = AnchorStyles.Left;
            repositoryTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                MinimumSize = new System.Drawing.Size(0, 44),
                Margin = Padding.Empty,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 11F)
            };
            var urlHint = MakeLabel("例如：https://github.com/用户名/仓库名", 0, 0, false);
            urlHint.ForeColor = System.Drawing.Color.FromArgb(112, 122, 138);
            urlHint.AutoSize = true;
            urlHint.Margin = Padding.Empty;
            urlHint.Anchor = AnchorStyles.Left;

            var folderLabel = MakeLabel("下载到文件夹", 0, 0, true);
            folderLabel.AutoSize = true;
            folderLabel.Margin = Padding.Empty;
            folderLabel.Anchor = AnchorStyles.Left;
            var folderRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                MinimumSize = new System.Drawing.Size(0, 44),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            folderRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            folderRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            destinationTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                MinimumSize = new System.Drawing.Size(0, 44),
                Margin = Padding.Empty,
                Font = new System.Drawing.Font("Microsoft YaHei UI", 10F),
                Text = GetSavedDownloadDirectory()
            };
            browseButton = MakeButton("选择文件夹…", 0, 0, 112, 38, false);
            browseButton.Dock = DockStyle.Fill;
            browseButton.Margin = new Padding(12, 0, 0, 0);
            browseButton.Click += BrowseButton_Click;
            folderRow.Controls.Add(destinationTextBox, 0, 0);
            folderRow.Controls.Add(browseButton, 1, 0);

            var actionRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                MinimumSize = new System.Drawing.Size(0, 48),
                Margin = Padding.Empty,
                Padding = new Padding(0, 5, 0, 0)
            };
            startButton = MakeButton("获取应用", 0, 0, 154, 40, true);
            startButton.Margin = Padding.Empty;
            startButton.Click += StartButton_Click;
            cancelButton = MakeButton("取消", 0, 0, 92, 40, false);
            cancelButton.Margin = new Padding(12, 0, 0, 0);
            cancelButton.Enabled = false;
            cancelButton.Click += CancelButton_Click;
            openFolderButton = MakeButton("打开下载目录", 0, 0, 166, 40, false);
            openFolderButton.Margin = new Padding(12, 0, 0, 0);
            openFolderButton.Enabled = false;
            openFolderButton.Click += OpenFolderButton_Click;
            actionRow.Controls.Add(startButton);
            actionRow.Controls.Add(cancelButton);
            actionRow.Controls.Add(openFolderButton);

            cardLayout.Controls.Add(urlLabel, 0, 0);
            cardLayout.Controls.Add(repositoryTextBox, 0, 1);
            cardLayout.Controls.Add(urlHint, 0, 2);
            cardLayout.Controls.Add(folderLabel, 0, 3);
            cardLayout.Controls.Add(folderRow, 0, 4);
            cardLayout.Controls.Add(actionRow, 0, 5);
            card.Controls.Add(cardLayout);
            body.Controls.Add(card, 0, 0);

            statusLabel = new Label
            {
                Text = "准备就绪",
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                ForeColor = System.Drawing.Color.FromArgb(50, 62, 80),
                AutoEllipsis = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            body.Controls.Add(statusLabel, 0, 1);

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Style = ProgressBarStyle.Continuous
            };
            body.Controls.Add(progressBar, 0, 2);

            var logLabel = MakeLabel("运行日志", 0, 0, true);
            logLabel.AutoSize = false;
            logLabel.Dock = DockStyle.Fill;
            logLabel.Margin = Padding.Empty;
            logLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            body.Controls.Add(logLabel, 0, 3);

            logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                BackColor = System.Drawing.Color.FromArgb(250, 251, 253),
                Font = new System.Drawing.Font("Consolas", 9.5F)
            };
            body.Controls.Add(logTextBox, 0, 4);

            rootLayout.Controls.Add(header, 0, 0);
            rootLayout.Controls.Add(body, 0, 1);
            Controls.Add(rootLayout);

            FormClosing += Form1_FormClosing;
        }

        private static Label MakeLabel(string text, int x, int y, bool bold)
        {
            return new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                AutoSize = true,
                ForeColor = System.Drawing.Color.FromArgb(40, 51, 68),
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular)
            };
        }

        private static Button MakeButton(string text, int x, int y, int width, int height, bool primary)
        {
            return new Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? System.Drawing.Color.FromArgb(41, 105, 214) : System.Drawing.Color.FromArgb(239, 243, 249),
                ForeColor = primary ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(47, 60, 78),
                Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, primary ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular),
                Cursor = Cursors.Hand
            };
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择保存 GitHub 项目的父文件夹";
                dialog.ShowNewFolderButton = true;
                if (Directory.Exists(destinationTextBox.Text))
                    dialog.SelectedPath = destinationTextBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    destinationTextBox.Text = dialog.SelectedPath;
                    if (!SaveDownloadDirectoryPreference())
                        MessageBox.Show(this, "无法在软件所在文件夹写入配置。请将软件放在可写入的位置，才能保存此路径。", "无法保存设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            RepositoryAddress repository;
            string destinationRoot;
            try
            {
                repository = ParseRepositoryAddress(repositoryTextBox.Text);
                destinationRoot = Path.GetFullPath(destinationTextBox.Text.Trim());
                if (string.IsNullOrWhiteSpace(destinationTextBox.Text))
                    throw new ArgumentException("请选择下载文件夹。");
                Directory.CreateDirectory(destinationRoot);
                if (!SaveDownloadDirectoryPreference(destinationRoot))
                    MessageBox.Show(this, "无法在软件所在文件夹写入配置。此次下载仍会继续，但下次启动时不会记住此路径。", "无法保存设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "输入有误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var repositoryDirectory = GetUniqueProjectDirectory(destinationRoot, SanitizePathPart(repository.Name));
            Directory.CreateDirectory(repositoryDirectory);
            latestProjectDirectory = repositoryDirectory;
            SetBusy(true);
            logTextBox.Clear();
            progressBar.Value = 0;
            cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            string temporaryArchive = Path.Combine(Path.GetTempPath(), "github-project-" + Guid.NewGuid().ToString("N") + ".zip");
            string projectDirectory = null;

            try
            {
                Log("仓库：" + repository.Owner + "/" + repository.Name);
                Log("正在检查 GitHub 最新正式 Release…");
                GitHubReleaseMetadata release = null;
                var releaseLookupFailed = false;
                try
                {
                    release = await GetLatestReleaseAsync(repository, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    releaseLookupFailed = true;
                    Log("Release 信息暂时无法读取；仍会继续下载源码：" + ex.Message);
                }

                var selectedReleaseAsset = release == null ? null : SelectReleaseAsset(release.Assets);
                GitHubReleaseAsset releaseAsset = null;
                string releaseDirectory = null;
                if (selectedReleaseAsset != null)
                {
                    var releaseTag = SanitizePathPart(string.IsNullOrWhiteSpace(release.TagName) ? release.Name : release.TagName);
                    releaseDirectory = GetUniqueProjectDirectory(repositoryDirectory, "Release-" + releaseTag);
                    Directory.CreateDirectory(releaseDirectory);

                    var assetFileName = SanitizeFileName(selectedReleaseAsset.Name);
                    var assetPath = Path.Combine(releaseDirectory, assetFileName);
                    try
                    {
                        Log("找到已发布版本：" + (string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name));
                        Log("下载发布附件：" + assetFileName);
                        Log("发布版保存到：" + releaseDirectory);
                        SetStatus("正在下载 GitHub Release…");
                        progressBar.Value = 0;
                        var releaseProgress = CreateDownloadProgress("正在下载 GitHub Release…", 0, 35);
                        await DownloadArchiveAsync(selectedReleaseAsset.BrowserDownloadUrl, assetPath, releaseProgress, token);

                        if (IsZipAsset(assetFileName))
                        {
                            var applicationDirectory = Path.Combine(releaseDirectory, "应用文件");
                            Directory.CreateDirectory(applicationDirectory);
                            SetStatus("正在解压发布版…");
                            Log("正在解压发布包到：" + applicationDirectory);
                            await Task.Run(() => ExtractArchive(assetPath, applicationDirectory, token), token);
                            Log("发布包已解压。原始压缩包保留在：" + assetPath);
                            progressBar.Value = 38;
                        }
                        else if (IsArchiveAsset(assetFileName))
                        {
                            Log("已下载发布包。此压缩格式不能自动解压，请在发布版目录中手动解压。");
                        }

                        progressBar.Style = ProgressBarStyle.Continuous;
                        progressBar.Value = Math.Max(progressBar.Value, 38);
                        releaseAsset = selectedReleaseAsset;
                        Log("GitHub 发布应用已下载；继续下载仓库源码。");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log("Release 附件下载或解压失败：" + ex.Message);
                        Log("将保留源码下载流程，并尝试从源码本机构建。");
                        try
                        {
                            if (Directory.Exists(releaseDirectory))
                                Directory.Delete(releaseDirectory, true);
                        }
                        catch { }
                        releaseDirectory = null;
                        progressBar.Style = ProgressBarStyle.Continuous;
                        progressBar.Value = 0;
                    }
                }

                if (releaseAsset == null)
                {
                    if (releaseLookupFailed)
                        Log("无法确认是否存在正式 Release，继续下载源码并尝试本机构建。");
                    else if (release == null)
                        Log("仓库没有正式 Release，改为下载源码并在本机构建。");
                    else if (selectedReleaseAsset == null)
                        Log("最新 Release 没有可识别的 Windows 应用附件，改为下载源码并在本机构建。");
                    else
                        Log("Release 附件未能使用，改为下载源码并在本机构建。");
                }

                Log("正在读取仓库默认分支…");
                var branch = await GetDefaultBranchAsync(repository, token);
                Log("默认分支：" + branch);

                var progress = releaseAsset == null
                    ? CreateDownloadProgress("正在下载源码…", 0, 60)
                    : CreateDownloadProgress("正在下载源码…", 40, 72);
                SetStatus("正在下载源码…");
                var archiveUrl = "https://api.github.com/repos/" + repository.Owner + "/" + repository.Name + "/zipball/" + Uri.EscapeDataString(branch);
                await DownloadArchiveAsync(archiveUrl, temporaryArchive, progress, token);
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = releaseAsset == null ? 62 : 73;
                token.ThrowIfCancellationRequested();

                projectDirectory = GetUniqueProjectDirectory(repositoryDirectory, "Source");
                Directory.CreateDirectory(projectDirectory);
                SetStatus("正在解压仓库…");
                Log("正在解压到：" + projectDirectory);
                await Task.Run(() => ExtractArchive(temporaryArchive, projectDirectory, token), token);
                progressBar.Value = releaseAsset == null ? 68 : 75;
                Log("源码已下载到：" + projectDirectory);

                if (releaseAsset != null)
                {
                    progressBar.Value = 100;
                    SetStatus("Release 应用和源码都已下载。");
                    Log("GitHub Release " + (release.TagName ?? release.Name) + " 应用包：" + releaseDirectory);
                    Log("源码目录：" + projectDirectory);
                    Log("本次未重复构建；官方发布版与源码已分别保存。");
                    return;
                }

                SetStatus("正在查找 .NET 项目…");
                var buildTarget = await Task.Run(() => FindBuildTarget(projectDirectory), token);
                if (buildTarget == null)
                {
                    progressBar.Value = 100;
                    SetStatus("源码已下载；未找到 .sln 或 .csproj，无法自动构建。");
                    Log("未找到 Visual Studio 解决方案（.sln）或 C# 项目（.csproj）。");
                    Log("源码已保存在：" + projectDirectory);
                    return;
                }

                Log("构建目标：" + buildTarget);
                var msbuild = FindMsBuild();
                string executable;
                string arguments;
                if (msbuild != null)
                {
                    executable = msbuild;
                    arguments = "/nologo /v:minimal /p:Configuration=Release " + Quote(buildTarget);
                    Log("使用 MSBuild 构建 Release 版本…");
                }
                else
                {
                    executable = "dotnet";
                    arguments = "build " + Quote(buildTarget) + " --configuration Release --nologo";
                    Log("未找到 Visual Studio MSBuild，改用 .NET SDK dotnet build…");
                }

                SetStatus("正在构建 Release 可执行文件…");
                progressBar.Value = 70;
                var buildStarted = DateTime.Now;
                var exitCode = await RunBuildProcessAsync(executable, arguments, projectDirectory, token);
                if (exitCode != 0)
                {
                    SetStatus("构建失败；源码已下载，可查看日志。");
                    Log("构建进程退出代码：" + exitCode);
                    Log("请确认本机已安装该项目所需的 .NET SDK、目标框架或 Visual Studio 工作负载。");
                    return;
                }

                progressBar.Value = 100;
                var outputs = FindExecutableOutputs(projectDirectory, buildStarted);
                if (outputs.Count > 0)
                {
                    var deploymentDirectory = GetUniqueProjectDirectory(repositoryDirectory, "LocalBuild");
                    Directory.CreateDirectory(deploymentDirectory);
                    SetStatus("正在将本机构建结果部署到指定目录…");
                    var deployedOutputs = await Task.Run(() => DeployExecutableOutputs(outputs, deploymentDirectory, token), token);
                    SetStatus("本地构建版已部署，找到 " + deployedOutputs.Count + " 个可执行文件。");
                    Log("本地构建版已部署到：" + deploymentDirectory);
                    Log("源码目录：" + projectDirectory);
                    Log("Release 可执行文件：");
                    foreach (var output in deployedOutputs.Take(10))
                        Log("  " + output);
                }
                else
                {
                    SetStatus("构建成功，但没有发现 .exe 文件（项目可能是类库）。");
                    Log("构建成功，但没有在 Release 输出中找到 .exe。若项目是类库，它不会生成独立可执行程序。");
                    Log("源码保存在：" + projectDirectory);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus(projectDirectory == null ? "操作已取消。" : "操作已取消；已下载的文件保留在目标文件夹。 ");
                Log("操作已取消。");
                if (!string.IsNullOrEmpty(latestProjectDirectory) && Directory.Exists(latestProjectDirectory))
                    openFolderButton.Enabled = true;
            }
            catch (HttpRequestException ex)
            {
                SetStatus("下载失败。");
                Log("网络请求失败：" + ex.Message);
                Log("请确认仓库可公开访问且网络正常；发布附件的下载链接也可能已失效。");
            }
            catch (Exception ex)
            {
                SetStatus("处理失败；请查看日志。");
                Log(ex.GetType().Name + "：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryArchive))
                        File.Delete(temporaryArchive);
                }
                catch { }
                if (cancellationSource != null)
                {
                    cancellationSource.Dispose();
                    cancellationSource = null;
                }
                SetBusy(false);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (cancellationSource != null)
            {
                cancelButton.Enabled = false;
                SetStatus("正在取消…");
                cancellationSource.Cancel();
                try
                {
                    if (activeBuildProcess != null && !activeBuildProcess.HasExited)
                        activeBuildProcess.Kill();
                }
                catch { }
            }
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(latestProjectDirectory) && Directory.Exists(latestProjectDirectory))
                Process.Start(new ProcessStartInfo("explorer.exe", Quote(latestProjectDirectory)) { UseShellExecute = true });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveDownloadDirectoryPreference();
            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
                try
                {
                    if (activeBuildProcess != null && !activeBuildProcess.HasExited)
                        activeBuildProcess.Kill();
                }
                catch { }
            }
        }

        private static string GetSavedDownloadDirectory()
        {
            var defaultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
            try
            {
                var settingsPath = GetPortableSettingsPath();
                if (File.Exists(settingsPath))
                {
                    var configured = File.ReadAllText(settingsPath, Encoding.UTF8).Trim();
                    if (!string.IsNullOrWhiteSpace(configured))
                        return configured;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return defaultDirectory;
        }

        private bool SaveDownloadDirectoryPreference()
        {
            return SaveDownloadDirectoryPreference(destinationTextBox.Text);
        }

        private static bool SaveDownloadDirectoryPreference(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                return false;
            try
            {
                File.WriteAllText(GetPortableSettingsPath(), directory.Trim(), new UTF8Encoding(false));
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static string GetPortableSettingsPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GetGithubProject.settings");
        }

        private void SetBusy(bool busy)
        {
            startButton.Enabled = !busy;
            repositoryTextBox.Enabled = !busy;
            destinationTextBox.Enabled = !busy;
            browseButton.Enabled = !busy;
            cancelButton.Enabled = busy;
            openFolderButton.Enabled = !busy && !string.IsNullOrEmpty(latestProjectDirectory) && Directory.Exists(latestProjectDirectory);
            if (!busy && progressBar.Style == ProgressBarStyle.Marquee)
                progressBar.Style = ProgressBarStyle.Continuous;
        }

        private void SetStatus(string message)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
                BeginInvoke((Action)(() => statusLabel.Text = message));
            else
                statusLabel.Text = message;
        }

        private void Log(string message)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Log(message)));
                return;
            }
            if (logTextBox.TextLength > 28000)
                logTextBox.Text = logTextBox.Text.Substring(logTextBox.TextLength - 18000);
            logTextBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }

        private static RepositoryAddress ParseRepositoryAddress(string value)
        {
            Uri uri;
            if (!Uri.TryCreate((value ?? "").Trim(), UriKind.Absolute, out uri) || uri.Scheme != Uri.UriSchemeHttps ||
                (uri.Host != "github.com" && uri.Host != "www.github.com"))
                throw new ArgumentException("请输入有效的 GitHub 仓库地址，例如：https://github.com/用户名/仓库名");

            var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                throw new ArgumentException("仓库地址需要包含用户名和仓库名。");
            var owner = Uri.UnescapeDataString(segments[0]);
            var name = Uri.UnescapeDataString(segments[1]);
            if (name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            if (!IsRepositorySegment(owner) || !IsRepositorySegment(name))
                throw new ArgumentException("仓库地址中的用户名或仓库名格式不正确。");
            return new RepositoryAddress { Owner = owner, Name = name };
        }

        private static bool IsRepositorySegment(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value != "." && value != ".." && Regex.IsMatch(value, "^[A-Za-z0-9_.-]+$");
        }

        private static async Task<string> GetDefaultBranchAsync(RepositoryAddress repository, CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var client = CreateHttpClient())
            using (var response = await client.GetAsync("https://api.github.com/repos/" + repository.Owner + "/" + repository.Name,
                HttpCompletionOption.ResponseHeadersRead, token))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new HttpRequestException("仓库不存在、不是公开仓库，或 GitHub 未公开其内容。");
                if (response.StatusCode == (HttpStatusCode)403)
                    throw new HttpRequestException("GitHub 暂时限制了请求次数，请稍后再试。");
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRepositoryMetadata));
                    var metadata = (GitHubRepositoryMetadata)serializer.ReadObject(stream);
                    if (metadata == null || string.IsNullOrWhiteSpace(metadata.DefaultBranch))
                        throw new InvalidDataException("GitHub 未返回仓库默认分支。");
                    return metadata.DefaultBranch;
                }
            }
        }

        private static async Task<GitHubReleaseMetadata> GetLatestReleaseAsync(RepositoryAddress repository, CancellationToken token)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var url = "https://api.github.com/repos/" + repository.Owner + "/" + repository.Name + "/releases/latest";
            using (var client = CreateHttpClient())
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                if (response.StatusCode == (HttpStatusCode)403)
                    throw new HttpRequestException("GitHub 暂时限制了请求次数，请稍后再试。");
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubReleaseMetadata));
                    return (GitHubReleaseMetadata)serializer.ReadObject(stream);
                }
            }
        }

        private static GitHubReleaseAsset SelectReleaseAsset(IEnumerable<GitHubReleaseAsset> assets)
        {
            if (assets == null)
                return null;
            GitHubReleaseAsset selected = null;
            var selectedScore = 0;
            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.Name) || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
                    continue;
                Uri url;
                if (!Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out url) || url.Scheme != Uri.UriSchemeHttps ||
                    !(url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                      url.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                      url.Host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var score = GetReleaseAssetScore(asset.Name);
                if (score > selectedScore)
                {
                    selected = asset;
                    selectedScore = score;
                }
            }
            return selected;
        }

        private static int GetReleaseAssetScore(string fileName)
        {
            var name = fileName.ToLowerInvariant();
            if (name.Contains("source") || name.Contains("checksum") || name.Contains("sha256") || name.EndsWith(".sig") || name.EndsWith(".asc"))
                return 0;
            if (name.Contains("linux") || name.Contains("darwin") || name.Contains("macos") || name.Contains("osx"))
                return 0;

            var extension = name.EndsWith(".tar.gz", StringComparison.Ordinal) ? ".tar.gz" : Path.GetExtension(name);
            int score;
            switch (extension)
            {
                case ".exe": score = 100; break;
                case ".msi": score = 95; break;
                case ".msix":
                case ".appx": score = 90; break;
                case ".zip": score = 85; break;
                case ".7z": score = 80; break;
                case ".rar": score = 75; break;
                case ".tar.gz":
                case ".tgz": score = 70; break;
                default: return 0;
            }

            if (name.Contains("windows") || name.Contains("win64") || name.Contains("win32") || name.Contains("-win") || name.Contains("_win"))
                score += 20;
            if (Environment.Is64BitOperatingSystem)
            {
                if (name.Contains("x64") || name.Contains("amd64") || name.Contains("win64") || name.Contains("x86_64"))
                    score += 15;
                if ((name.Contains("x86") && !name.Contains("x86_64")) || name.Contains("i386"))
                    score -= 15;
            }
            else
            {
                if (name.Contains("x86") || name.Contains("i386") || name.Contains("win32"))
                    score += 15;
                if (name.Contains("x64") || name.Contains("amd64") || name.Contains("win64"))
                    score -= 15;
            }
            return score;
        }

        private IProgress<DownloadProgress> CreateDownloadProgress(string operation, int progressStart, int progressEnd)
        {
            return new Progress<DownloadProgress>(p =>
            {
                if (p.TotalBytes > 0)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                    var range = progressEnd - progressStart;
                    var value = progressStart + (int)(p.BytesReceived * range / p.TotalBytes);
                    progressBar.Value = Math.Max(progressStart, Math.Min(progressEnd, value));
                    SetStatus(operation + " " + FormatBytes(p.BytesReceived) + " / " + FormatBytes(p.TotalBytes));
                }
                else
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                    SetStatus(operation + " " + FormatBytes(p.BytesReceived));
                }
            });
        }

        private static async Task DownloadArchiveAsync(string url, string destination, IProgress<DownloadProgress> progress, CancellationToken token)
        {
            using (var client = CreateHttpClient())
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? -1;
                using (var input = await response.Content.ReadAsStreamAsync())
                using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    long received = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, token);
                        received += read;
                        progress.Report(new DownloadProgress { BytesReceived = received, TotalBytes = total });
                    }
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GetGithubProject/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static string GetUniqueProjectDirectory(string parent, string repositoryName)
        {
            var candidate = Path.Combine(parent, repositoryName);
            var suffix = 2;
            while (Directory.Exists(candidate) || File.Exists(candidate))
                candidate = Path.Combine(parent, repositoryName + " (" + suffix++ + ")");
            return candidate;
        }

        private static void ExtractArchive(string archivePath, string destination, CancellationToken token)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                var entries = archive.Entries;
                var firstSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hasFilesAtRoot = false;
                foreach (var entry in entries)
                {
                    var normalized = entry.FullName.Replace('\\', '/');
                    var slash = normalized.IndexOf('/');
                    if (slash < 0)
                        hasFilesAtRoot = true;
                    else if (slash > 0)
                        firstSegments.Add(normalized.Substring(0, slash));
                }
                var stripRoot = !hasFilesAtRoot && firstSegments.Count == 1;
                var rootPrefix = stripRoot ? firstSegments.First() + "/" : null;
                var destinationRoot = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                foreach (var entry in entries)
                {
                    token.ThrowIfCancellationRequested();
                    var entryName = entry.FullName.Replace('\\', '/');
                    if (stripRoot)
                    {
                        if (!entryName.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                            continue;
                        entryName = entryName.Substring(rootPrefix.Length);
                    }
                    if (string.IsNullOrWhiteSpace(entryName))
                        continue;
                    var relativePath = entryName.Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.GetFullPath(Path.Combine(destinationRoot, relativePath));
                    if (!fullPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("压缩包包含不安全的路径，已停止解压。");

                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }
                    var parent = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(parent))
                        Directory.CreateDirectory(parent);
                    using (var input = entry.Open())
                    using (var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            output.Write(buffer, 0, read);
                        }
                    }
                }
            }
        }

        private static string FindBuildTarget(string root)
        {
            var solutions = new List<string>();
            var projects = new List<string>();
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                try
                {
                    solutions.AddRange(Directory.GetFiles(current, "*.sln", SearchOption.TopDirectoryOnly));
                    projects.AddRange(Directory.GetFiles(current, "*.csproj", SearchOption.TopDirectoryOnly));
                    foreach (var directory in Directory.GetDirectories(current))
                    {
                        var name = Path.GetFileName(directory);
                        if (name.Equals(".git", StringComparison.OrdinalIgnoreCase) || name.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || name.Equals("packages", StringComparison.OrdinalIgnoreCase))
                            continue;
                        pending.Push(directory);
                    }
                }
                catch (UnauthorizedAccessException) { }
            }
            return solutions.OrderBy(p => p.Length).FirstOrDefault() ?? projects.OrderBy(p => p.Length).FirstOrDefault();
        }

        private static string FindMsBuild()
        {
            var pathCandidate = FindOnPath("MSBuild.exe");
            if (pathCandidate != null)
                return pathCandidate;

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var vswhere = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (File.Exists(vswhere))
            {
                try
                {
                    var info = new ProcessStartInfo(vswhere, "-latest -products * -requires Microsoft.Component.MSBuild -find \"MSBuild\\**\\Bin\\MSBuild.exe\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var process = Process.Start(info))
                    {
                        var result = process.StandardOutput.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        process.WaitForExit();
                        if (!string.IsNullOrWhiteSpace(result) && File.Exists(result.Trim()))
                            return result.Trim();
                    }
                }
                catch { }
            }

            foreach (var basePath in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), programFilesX86 })
            {
                var visualStudio = Path.Combine(basePath, "Microsoft Visual Studio");
                if (!Directory.Exists(visualStudio))
                    continue;
                try
                {
                    foreach (var version in Directory.GetDirectories(visualStudio))
                        foreach (var edition in Directory.GetDirectories(version))
                        {
                            var candidate = Path.Combine(edition, "MSBuild", "Current", "Bin", "MSBuild.exe");
                            if (File.Exists(candidate))
                                return candidate;
                        }
                }
                catch { }
            }
            return null;
        }

        private static string FindOnPath(string fileName)
        {
            foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                var candidate = Path.Combine(path.Trim('"'), fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        private async Task<int> RunBuildProcessAsync(string executable, string arguments, string workingDirectory, CancellationToken token)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                var completion = new TaskCompletionSource<int>();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    try { completion.TrySetResult(process.ExitCode); }
                    catch (InvalidOperationException) { completion.TrySetResult(-1); }
                };
                token.ThrowIfCancellationRequested();
                if (!process.Start())
                    throw new InvalidOperationException("无法启动构建工具：" + executable);
                activeBuildProcess = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                using (token.Register(() =>
                {
                    try { if (!process.HasExited) process.Kill(); }
                    catch { }
                }))
                {
                    var result = await completion.Task;
                    process.WaitForExit();
                    token.ThrowIfCancellationRequested();
                    return result;
                }
            }
        }

        private static List<string> FindExecutableOutputs(string root, DateTime buildStarted)
        {
            var outputs = new List<string>();
            foreach (var file in Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories))
            {
                var relative = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.IndexOf("\\bin\\release\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    relative.IndexOf("/bin/release/", StringComparison.OrdinalIgnoreCase) < 0 &&
                    relative.IndexOf("\\release\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    relative.IndexOf("/release/", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                try
                {
                    if (File.GetLastWriteTime(file) >= buildStarted.AddSeconds(-3))
                        outputs.Add(file);
                }
                catch { }
            }
            return outputs.OrderByDescending(File.GetLastWriteTime).Take(20).ToList();
        }

        private static List<string> DeployExecutableOutputs(IEnumerable<string> executablePaths, string destination, CancellationToken token)
        {
            var groups = executablePaths
                .GroupBy(Path.GetDirectoryName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var deployed = new List<string>();
            Directory.CreateDirectory(destination);

            if (groups.Count == 1)
            {
                var outputDirectory = groups[0].Key;
                CopyDirectory(outputDirectory, destination, token);
                deployed.AddRange(groups[0].Select(file => Path.Combine(destination, Path.GetFileName(file))));
                return deployed;
            }

            foreach (var group in groups)
            {
                token.ThrowIfCancellationRequested();
                var folderName = SanitizePathPart(Path.GetFileNameWithoutExtension(group.First()));
                var projectOutput = GetUniqueProjectDirectory(destination, folderName);
                CopyDirectory(group.Key, projectOutput, token);
                deployed.AddRange(group.Select(file => Path.Combine(projectOutput, Path.GetFileName(file))));
            }
            return deployed;
        }

        private static void CopyDirectory(string source, string destination, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                token.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }
            foreach (var directory in Directory.GetDirectories(source))
            {
                token.ThrowIfCancellationRequested();
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    continue;
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)), token);
            }
        }

        private static bool IsZipAsset(string fileName)
        {
            return fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsArchiveAsset(string fileName)
        {
            return IsZipAsset(fileName) || fileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeFileName(string value)
        {
            var fileName = Path.GetFileName((value ?? "").Replace('\\', '/'));
            foreach (var invalid in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalid, '-');
            fileName = fileName.Trim().TrimEnd('.', ' ');
            return string.IsNullOrWhiteSpace(fileName) ? "release-asset" : fileName;
        }

        private static string SanitizePathPart(string value)
        {
            var safe = SanitizeFileName(value);
            return safe.Length > 80 ? safe.Substring(0, 80) : safe;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string FormatBytes(long value)
        {
            if (value >= 1024L * 1024L * 1024L)
                return (value / (1024d * 1024d * 1024d)).ToString("0.0") + " GB";
            if (value >= 1024L * 1024L)
                return (value / (1024d * 1024d)).ToString("0.0") + " MB";
            if (value >= 1024L)
                return (value / 1024d).ToString("0") + " KB";
            return value + " B";
        }

        private sealed class RepositoryAddress
        {
            public string Owner { get; set; }
            public string Name { get; set; }
        }

        private sealed class DownloadProgress
        {
            public long BytesReceived { get; set; }
            public long TotalBytes { get; set; }
        }

        [DataContract]
        private sealed class GitHubRepositoryMetadata
        {
            [DataMember(Name = "default_branch")]
            public string DefaultBranch { get; set; }
        }

        [DataContract]
        private sealed class GitHubReleaseMetadata
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "assets")]
            public List<GitHubReleaseAsset> Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubReleaseAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
