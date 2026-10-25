# GitHub 项目下载器

Windows 桌面工具：粘贴公开 GitHub 仓库地址并选择保存位置。每次都会下载源码；如果有可用的正式 Release 应用，也会同时下载发布包。没有可用发布包时，再从源码构建 .NET 项目。

## 使用方法

1. 启动 `bin\Release\GetGithubProject.exe`。
2. 粘贴仓库地址，例如 `https://github.com/owner/repository`。
3. 选择保存目录，点击“获取应用”。
4. 在运行日志中查看结果。“打开下载目录”会打开本次结果目录。

输出目录会标明来源：`仓库名-Release-版本号` 是 GitHub 发布版，`仓库名-Source` 是源码，`仓库名-LocalBuild` 是从源码生成并部署到本机的构建版。同名目录已存在时会自动添加编号。“打开下载目录”和“获取应用”按钮在同一行，打开所选的目标目录。

## 支持范围

- 目前支持公开 GitHub 仓库，并检查最新正式 Release（不包含草稿和预发布版本）。
- 从 Release 附件中选择 Windows/通用 `.exe`、`.msi`、`.msix`、`.appx`、`.zip`、`.7z`、`.rar`、`.tar.gz` 或 `.tgz` 包；`.zip` 会自动解压，其它安装包/压缩包会原样保存，需用户自行安装或解压。程序不会自动运行安装程序。
- 无论是否找到 Release 应用附件，都会下载仓库源码到 `仓库名-Source`。
- 没有正式 Release 或没有可识别的 Windows 应用附件时，自动构建仓库中的 `.sln` 或 `.csproj`，优先调用 Visual Studio MSBuild；找不到时使用 .NET SDK。
- 本机需要安装项目所需的 .NET SDK、目标框架或 Visual Studio 工作负载。构建出的 Release 程序和依赖文件会复制到 `仓库名-LocalBuild`。
- 类库构建成功也可能不会产生 `.exe`。非 .NET 仓库需要相应语言的构建工具和构建步骤，本工具不会自动将任意源码转换成 Windows 程序。
- 下载公开仓库源码需要网络连接；GitHub API 请求受其公开速率限制影响。

## 构建本工具

使用 Visual Studio 打开 `GetGithubProject.slnx`，选择 `Release` 并生成。应用以 .NET Framework 4.8 为目标，运行电脑需要具备 .NET Framework 4.8。
