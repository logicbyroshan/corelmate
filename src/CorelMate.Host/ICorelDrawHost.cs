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
        var application = (CorelApplication)Marshal.GetActiveObject("CorelDRAW.Application");
        return new CorelDrawHost(application);
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