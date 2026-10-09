using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;

namespace Trizbort.Tests {
  [TestFixture, Category("Unit")]
  public class NamingConventionTests {
    private const BindingFlags DeclaredMembers =
      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
      BindingFlags.Static | BindingFlags.DeclaredOnly;

    [TestCase(false)]
    [TestCase(true)]
    public void Members_UseModernNamingConventions(bool testAssembly) {
      var assembly = testAssembly ? typeof(NamingConventionTests).Assembly : typeof(Project).Assembly;
      var violations = new List<string>();
      foreach (var type in assembly.GetTypes().Where(type =>
        !type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
        type.Namespace != "Trizbort.Properties")) {
        foreach (var field in type.GetFields(DeclaredMembers).Where(field =>
          !field.IsDefined(typeof(CompilerGeneratedAttribute), false))) {
          if (field.IsLiteral) {
            if (!char.IsUpper(field.Name[0]) || field.Name.Contains('_'))
              violations.Add(type.Name + "." + field.Name);
          } else if (field.IsPrivate &&
            (!Regex.IsMatch(field.Name, "^_[a-z]") || Regex.IsMatch(field.Name, "^_[ms][A-Z_]"))) {
            violations.Add(type.Name + "." + field.Name);
          }
        }
        foreach (var method in type.GetMethods(DeclaredMembers).Where(method =>
          !method.IsSpecialName && !method.IsDefined(typeof(CompilerGeneratedAttribute), false))) {
          if (!char.IsUpper(method.Name[0])) violations.Add(type.Name + "." + method.Name);
        }
      }
      violations.ShouldBeEmpty();
    }
  }
}
