using System;
using System.Runtime.InteropServices;
using CorelApplication = Corel.Interop.CorelDRAW.Application;
using CorelDocument = Corel.Interop.VGCore.Document;
using CorelLayer = Corel.Interop.VGCore.Layer;
using CorelPage = Corel.Interop.VGCore.Page;
using CorelShapeRange = Corel.Interop.VGCore.ShapeRange;

namespace CorelMate.Host;

public interface ICorelDrawHost
{
    CorelApplication Application { get; }
    CorelDocument? ActiveDocument { get; }
    CorelPage? ActivePage { get; }
    CorelLayer? ActiveLayer { get; }
    CorelShapeRange? ActiveSelection { get; }

    string HostVersionString { get; }
    int HostMajorVersion { get; }
    string? ActiveDocumentSummary { get; }
}

public sealed class CorelDrawHost : ICorelDrawHost
{
    public CorelDrawHost(CorelApplication application) => Application = application ?? throw new ArgumentNullException(nameof(application));

    public static CorelDrawHost ConnectToRunningInstance()
    {
        // 1. Try ROT (Running Object Table) first
        try
        {
            var app = (CorelApplication)Marshal.GetActiveObject("CorelDRAW.Application");
            if (app != null) return new CorelDrawHost(app);
        }
        catch
        {
        }

        // 2. CorelDRAW is a single-instance COM server.
        // Activator.CreateInstance connects to the active running instance if CorelDRAW is running.
        var appType = Type.GetTypeFromProgID("CorelDRAW.Application");
        if (appType != null)
        {
            try
            {
                var comObj = Activator.CreateInstance(appType);
                if (comObj != null)
                {
                    return new CorelDrawHost((CorelApplication)comObj);
                }
            }
            catch
            {
            }
        }

        // 3. Fallback to version-specific ProgIDs (v27 down to v19)
        for (var ver = 28; ver >= 19; ver--)
        {
            try
            {
                var vType = Type.GetTypeFromProgID($"CorelDRAW.Application.{ver}");
                if (vType != null)
                {
                    var comObj = Activator.CreateInstance(vType);
                    if (comObj != null) return new CorelDrawHost((CorelApplication)comObj);
                }
            }
            catch
            {
            }
        }

        throw new InvalidOperationException("Could not connect to CorelDRAW instance. Ensure CorelDRAW is running.");
    }

    public static CorelDrawHost ConnectToComObject(object comObject)
    {
        if (comObject == null) throw new ArgumentNullException(nameof(comObject));
        var unknown = Marshal.GetIUnknownForObject(comObject);
        try
        {
            var application = (CorelApplication)Marshal.GetTypedObjectForIUnknown(unknown, typeof(CorelApplication));
            return new CorelDrawHost(application);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    public CorelApplication Application { get; }
    public CorelDocument? ActiveDocument => Application.ActiveDocument;
    public CorelPage? ActivePage => Application.ActivePage;
    public CorelLayer? ActiveLayer => Application.ActiveLayer;
    public CorelShapeRange? ActiveSelection => Application.ActiveSelectionRange;

    public string HostVersionString
    {
        get
        {
            try { return Application.Version; }
            catch { return string.Empty; }
        }
    }

    public int HostMajorVersion
    {
        get
        {
            var ver = CorelVersionDetector.ExtractMajorVersion(HostVersionString);
            return ver > 0 ? ver : 27;
        }
    }

    public string? ActiveDocumentSummary
    {
        get
        {
            try
            {
                var doc = ActiveDocument;
                return doc != null ? $"{doc.Name} ({doc.Pages.Count} page(s))" : null;
            }
            catch
            {
                return null;
            }
        }
    }
}