using System.Text;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Export;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///     Writes PNG, PDF and code exports of every sample map into TRIZBORT_COMPARE_OUTPUT/new/&lt;map&gt;/ so they can
///     be compared with the legacy app's output (scripts/compare/compare_exports.py). Explicit and env-var gated.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Capture")]
public class MapExportCaptureTests
{
    private const string OutputVariable = "TRIZBORT_COMPARE_OUTPUT";

    private static readonly (ExportLanguage Language, string FileName)[] Languages =
    {
        (ExportLanguage.Inform7, "inform7"),
        (ExportLanguage.Inform6, "inform6"),
        (ExportLanguage.Tads, "tads"),
        (ExportLanguage.Hugo, "hugo"),
        (ExportLanguage.Zil, "zil"),
        (ExportLanguage.Quest, "quest"),
        (ExportLanguage.Alan, "alan"),
        (ExportLanguage.Adventuron, "adventuron")
    };

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "samples")) &&
                Directory.Exists(Path.Combine(dir.FullName, "src")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    [AvaloniaTest]
    [Explicit("Writes comparison output; set TRIZBORT_COMPARE_OUTPUT")]
    public void CaptureAllMaps()
    {
        var outputRoot = Environment.GetEnvironmentVariable(OutputVariable);
        if (string.IsNullOrWhiteSpace(outputRoot)) Assert.Ignore("Set " + OutputVariable + " to capture exports.");

        var repo = FindRepoRoot();
        Assert.That(repo, Is.Not.Null, "Could not find the repository root.");

        var maps = new List<string>();
        foreach (var folder in new[] { "samples", Path.Combine("tests", "manual") })
        {
            var path = Path.Combine(repo!, folder);
            if (Directory.Exists(path)) maps.AddRange(Directory.GetFiles(path, "*.trizbort"));
        }

        maps.Sort(StringComparer.OrdinalIgnoreCase);

        var previousProject = Project.Current;
        try
        {
            foreach (var map in maps)
            {
                var outDir = Path.Combine(outputRoot!, "new", Path.GetFileNameWithoutExtension(map));
                CaptureMap(map, outDir);
            }
        }
        finally
        {
            Project.Current = previousProject;
        }
    }

    private static void CaptureMap(string mapPath, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var errors = new StringBuilder();
        var project = new Project();
        var previous = Project.Current;
        try
        {
            Project.Current = project;
            Settings.Reset();
            if (!new MapLoader(project).LoadMap(mapPath))
            {
                errors.AppendLine("load: MapLoader returned false");
                return;
            }

            // As File > Open does: the file name is the title fallback in code exports.
            project.FileName = mapPath;
            // Reference default (ApplicationSettingsController.ResetSettings); the test process never loads settings.
            Trizbort.Domain.AppSettings.ApplicationSettingsController.AppSettings.SaveTadstoAdv3Lite = true;

            try
            {
                var plan = ImageExportPlanner.Plan(
                    project, null, null, new ImageExportOptions { Scale = 1.0 },
                    ApplicationSettingsController.AppSettings);
                using var bitmap = MapCanvasControl.RenderToBitmap(project, plan);
                if (bitmap == null)
                {
                    errors.AppendLine("png: map is empty, nothing rendered");
                }
                else
                {
                    using var stream = new FileStream(Path.Combine(outDir, "map.png"), FileMode.Create, FileAccess.Write);
                    ImageEncoder.Encode(bitmap, ImageFormat.Png, stream);
                }
            }
            catch (Exception ex)
            {
                errors.AppendLine("png: " + ex);
            }

            try
            {
                new SkiaMapPdfWriter()
                    .WriteAsync(project, Path.Combine(outDir, "map.pdf"), PdfMetadata.FromProject(project))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                errors.AppendLine("pdf: " + ex);
            }

            foreach (var (language, name) in Languages)
            {
                try
                {
                    var target = Path.Combine(outDir, name + ".txt");
                    if (language == ExportLanguage.Inform7)
                    {
                        var temp = Path.Combine(Path.GetTempPath(), "trizbort-i7-" + Guid.NewGuid().ToString("N") + ".inform");
                        try
                        {
                            CodeExportService.ExportToFile(language, project, temp);
                            File.Copy(Path.Combine(temp, "Source", "Story.ni"), target, true);
                        }
                        finally
                        {
                            if (Directory.Exists(temp)) Directory.Delete(temp, true);
                        }
                    }
                    else
                    {
                        File.WriteAllText(target, CodeExportService.ExportToText(language, project));
                    }
                }
                catch (Exception ex)
                {
                    errors.AppendLine(name + ": " + ex);
                }
            }
        }
        catch (Exception ex)
        {
            errors.AppendLine("map: " + ex);
        }
        finally
        {
            Settings.Reset();
            Project.Current = previous;
            project.Dispose();
            if (errors.Length > 0) File.WriteAllText(Path.Combine(outDir, "errors.txt"), errors.ToString());
        }
    }
}
