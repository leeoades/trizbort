using System;
using CommandLine;

namespace Trizbort.Domain.Application;

/// <summary>
///   Single entry point for parsing command-line arguments. Never throws and never returns null;
///   errors and unknown options are ignored (no help text), as in the reference application.
/// </summary>
public static class CommandLineParsing
{
  /// <summary>
  ///   Parses <paramref name="args" />, which must NOT include the executable path
  ///   (i.e. the arguments given to Main). The first positional argument is the file name.
  /// </summary>
  public static CommandLineOptions Parse(string[] args)
  {
    try
    {
      if (args == null) args = Array.Empty<string>();

      // CommandLineOptions declares the executable as positional value 0 (reference behaviour,
      // where Environment.GetCommandLineArgs() is parsed), so supply a placeholder for it.
      var full = new string[args.Length + 1];
      full[0] = "trizbort";
      Array.Copy(args, 0, full, 1, args.Length);

      using var parser = new Parser(settings =>
      {
        settings.HelpWriter = null;
        settings.IgnoreUnknownArguments = true;
      });

      var result = parser.ParseArguments<CommandLineOptions>(full);
      if (result is Parsed<CommandLineOptions> parsed) return parsed.Value ?? new CommandLineOptions();
    }
    catch (Exception)
    {
      // ignored: bad arguments must never prevent startup
    }

    return new CommandLineOptions();
  }
}
