using System;
using System.IO;
using System.Runtime.InteropServices;
using CorelApplication = Corel.Interop.CorelDRAW.Application;
using CorelMate.Core;
using CorelMate.Infrastructure;

namespace CorelMate.Host;

[ComVisible(true)]
[Guid("2AA0EFC8-22B7-4C80-9F62-7EAE8E0C7B1D")]
public sealed class CorelMateAddIn
{
    public const string DockerGuid = "{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}";
    public const string DockerClassName = "CorelMate.UI.CorelMatePanel";

    private readonly ICorelMateLogger logger;
    private CorelApplication? application;

    public CorelMateAddIn() : this(new FileCorelMateLogger()) { }

    internal CorelMateAddIn(ICorelMateLogger logger) => this.logger = logger;

    public void Initialize(CorelApplication hostApplication)
    {
        application = hostApplication ?? throw new ArgumentNullException(nameof(hostApplication));
        logger.Info($"CorelMate host initialized with CorelDRAW {application.Version}.");
    }

    public void RegisterDocker()
    {
        if (application == null) throw new InvalidOperationException("CorelMate has not been initialized with CorelDRAW.");
        var assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(CorelMateAddIn).Assembly.Location) ?? string.Empty, "CorelMate.UI.dll");
        application.FrameWork.AddDocker(DockerGuid, DockerClassName, assemblyPath);
        logger.Info("CorelMate Docker registered.");
    }

    public void ShowDocker()
    {
        if (application == null) throw new InvalidOperationException("CorelMate has not been initialized with CorelDRAW.");
        application.FrameWork.ShowDocker(DockerGuid);
        logger.Info("CorelMate Docker shown.");
    }

    public void HideDocker()
    {
        if (application == null) throw new InvalidOperationException("CorelMate has not been initialized with CorelDRAW.");
        application.FrameWork.HideDocker(DockerGuid);
        logger.Info("CorelMate Docker hidden.");
    }

    public bool IsDockerVisible()
    {
        if (application == null) return false;
        try
        {
            return application.FrameWork.IsDockerVisible(DockerGuid);
        }
        catch
        {
            return false;
        }
    }
}
