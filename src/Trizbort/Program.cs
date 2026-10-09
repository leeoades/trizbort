using System;
using System.Windows.Forms;
using CommandLine;
using Trizbort.Domain;
using Trizbort.UI;

namespace Trizbort
{
  internal static class Program
  {
    public static MainForm MainForm { get; private set; }

    /// <summary>
    ///   The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
      ApplicationConfiguration.Initialize();

      using var form = new MainForm();
      MainForm = form;
      Application.Run(form);
      MainForm = null;
    }
  }
}