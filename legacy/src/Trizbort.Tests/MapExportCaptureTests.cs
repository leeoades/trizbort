using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using PdfSharp.Drawing;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;
using Trizbort.Export;
using Trizbort.Export.Languages;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

/// <summary>
///   Export harness for the Avalonia parity comparison. Skipped unless TRIZBORT_COMPARE_OUTPUT names an output folder.
///   Loads every *.trizbort under samples and tests/manual with the legacy loader and writes, per map, into
///   &lt;output&gt;/legacy/&lt;map-name&gt;/: map.png (as File &gt; Export &gt; Image at 100%), map.pdf (MapPdfExporter), one file
///   per code exporter, and errors.txt. The MainForm is created but never shown (off-screen, transparent) so the
///   Canvas exists; no dialogs are shown.
///   Run on Windows with e.g. <c>TRIZBORT_COMPARE_OUTPUT=C:\cmp dotnet test --filter MapExportCaptureTests</c>.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
[Explicit]
[Category("Manual")]
public class MapExportCaptureTests : IsolatedProjectTests
{
  private const string OutputVariable = "TRIZBORT_COMPARE_OUTPUT";

  private string _output;

  [OneTimeSetUp]
  public void RequireOutputFolder()
  {
    _output = Environment.GetEnvironmentVariable(OutputVariable);
    if (string.IsNullOrWhiteSpace(_output))
      Assert.Ignore(OutputVariable + " is not set; map export capture skipped.");
    Directory.CreateDirectory(_output);
  }

  [Test]
  public void ExportAllMaps()
  {
    var root = FindRepositoryRoot();
    var maps = new List<string>();
    foreach (var folder in new[] { Path.Combine(root, "samples"), Path.Combine(root, "tests", "manual") })
      if (Directory.Exists(folder))
        maps.AddRange(Directory.GetFiles(folder, "*.trizbort").OrderBy(path => path, StringComparer.Ordinal));

    var previousForm = TrizbortApplication.MainForm;
    var previousInteraction = UserInteraction.Current;
    UserInteraction.Current = new SilentInteraction();
    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var form = new MainForm();
    try
    {
      form.ShowInTaskbar = false;
      form.Opacity = 0;
      form.StartPosition = FormStartPosition.Manual;
      form.Location = new Point(-32000, -32000);

      foreach (var map in maps)
      {
        var name = Path.GetFileNameWithoutExtension(map);
        while (!used.Add(name)) name += "-manual";
        var folder = Path.Combine(_output, "legacy", name);
        Directory.CreateDirectory(folder);
        var errors = new List<string>();
        try
        {
          ExportMap(form, map, folder, errors);
        }
        catch (Exception exception)
        {
          errors.Add("map: " + exception);
        }

        File.WriteAllText(Path.Combine(folder, "errors.txt"), string.Join(Environment.NewLine, errors));
      }
    }
    finally
    {
      form.Dispose();
      TrizbortApplication.MainForm = previousForm;
      UserInteraction.Current = previousInteraction;
    }

    Assert.Pass();
  }

  private static void ExportMap(MainForm form, string map, string folder, List<string> errors)
  {
    // Same steps as MainForm.OpenProject(string) minus the recent-files/settings side effect.
    Settings.Reset(false);
    ApplicationSettingsController.AppSettings.SaveAt100 = true;
    var previous = Project.Current;
    var project = new Project { FileName = map };
    if (!project.Load())
    {
      errors.Add("load: Project.Load returned false for " + map);
      project.Dispose();
      return;
    }

    Project.Current = project;
    if (previous != null && !ReferenceEquals(previous, project)) previous.Dispose();

    Guard(errors, "map.png", () => SaveImage(form.Canvas, Path.Combine(folder, "map.png")));
    Guard(errors, "map.pdf", () => MapPdfExporter.Save(form.Canvas, Path.Combine(folder, "map.pdf")));
    Guard(errors, "inform7", () => ExportInform7(folder));
    Guard(errors, "inform6", () => ExportCode<Inform6Exporter>(Path.Combine(folder, "inform6.txt")));
    Guard(errors, "tads", () => ExportCode<TadsExporter>(Path.Combine(folder, "tads.txt")));
    Guard(errors, "hugo", () => ExportCode<HugoExporter>(Path.Combine(folder, "hugo.txt")));
    Guard(errors, "zil", () => ExportCode<ZilExporter>(Path.Combine(folder, "zil.txt")));
    Guard(errors, "quest", () => ExportCode<QuestExporter>(Path.Combine(folder, "quest.txt")));
    Guard(errors, "alan", () => ExportCode<AlanExporter>(Path.Combine(folder, "alan.txt")));
    Guard(errors, "adventuron", () => ExportCode<AdventuronExporter>(Path.Combine(folder, "adventuron.txt")));
  }

  private static void Guard(List<string> errors, string what, Action action)
  {
    try
    {
      action();
    }
    catch (Exception exception)
    {
      errors.Add(what + ": " + exception.GetType().Name + ": " + exception.Message);
    }
  }

  private static void ExportCode<T>(string file) where T : CodeExporter, new()
  {
    // MainForm.ExportCode<T>(ref string) calls exporter.Export(fileName).
    using var exporter = new T();
    exporter.Export(file);
  }

  private static void ExportInform7(string folder)
  {
    // Inform7Exporter.Create writes <name>.inform/Source/Story.ni when the extension is ".inform".
    var project = Path.Combine(folder, "inform7-project.inform");
    using (var exporter = new Inform7Exporter())
    {
      exporter.Export(project);
    }

    File.Copy(Path.Combine(project, "Source", "Story.ni"), Path.Combine(folder, "inform7.txt"), true);
    Directory.Delete(project, true);
  }

  /// <summary>Copy of MainForm.SaveImage (PNG branch) with SaveAt100 forced on.</summary>
  private static void SaveImage(Canvas canvas, string fileName)
  {
    var size = canvas.ComputeCanvasBounds(true).Size *
               (ApplicationSettingsController.AppSettings.SaveAt100 ? 1.0f : canvas.ZoomFactor);
    size.X = Numeric.Clamp(size.X, 16, 8192);
    size.Y = Numeric.Clamp(size.Y, 16, 8192);

    using var bitmap = new Bitmap((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));
    using (var imageGraphics = Graphics.FromImage(bitmap))
    {
      using (var graphics = XGraphics.FromGraphics(imageGraphics, new XSize(size.X, size.Y)))
      {
        canvas.Draw(graphics, true, size.X, size.Y);
      }
    }

    bitmap.Save(fileName, ImageFormat.Png);
  }

  private static string FindRepositoryRoot()
  {
    var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (directory != null)
    {
      if (Directory.Exists(Path.Combine(directory.FullName, "samples"))
          && Directory.Exists(Path.Combine(directory.FullName, "legacy")))
        return directory.FullName;
      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("No folder containing both 'samples' and 'legacy' above the test directory.");
  }

  private sealed class SilentInteraction : IUserInteraction
  {
    public DialogResult ShowMessage(
      IWin32Window owner,
      string text,
      string caption,
      MessageBoxButtons buttons,
      MessageBoxIcon icon,
      MessageBoxDefaultButton defaultButton)
    {
      TestContext.Out.WriteLine("message suppressed: " + caption + ": " + text);
      return DialogResult.Cancel;
    }

    public DialogResult ShowDialog(Form dialog, IWin32Window owner)
    {
      return DialogResult.Cancel;
    }

    public DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner)
    {
      return DialogResult.Cancel;
    }

    public string GetClipboardText()
    {
      return string.Empty;
    }

    public void SetClipboardText(string text, TextDataFormat format)
    {
    }
  }
}
