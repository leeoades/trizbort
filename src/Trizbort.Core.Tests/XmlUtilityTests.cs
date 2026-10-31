using System.Drawing;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Misc;
using Trizbort.Util;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Unit")]
public class XmlUtilityTests
{
  [Test]
  public void XmlScribe_WritesStructuredXmlWithVariousTypes_AndRoundTripsViaReaders()
  {
    var tempFile = Path.GetTempFileName();
    try
    {
      using (var scribe = XmlScribe.Create(tempFile))
      {
        scribe.StartElement("root");
        scribe.Attribute("version", 2.5f);
        scribe.Attribute("active", true);
        scribe.Attribute("inactive", false);
        scribe.Attribute("color", Color.FromArgb(255, 128, 64));
        scribe.Attribute("dir", CompassPoint.NorthWest);
        scribe.Attribute("title", "Test Title");

        scribe.StartElement("child");
        scribe.Attribute("id", 42);
        scribe.Element("textNode", "Hello World");
        scribe.Element("numberNode", 123.45f);
        scribe.Element("flagTrue", true);
        scribe.Element("flagFalse", false);
        scribe.Element("colorNode", Color.Blue);
        scribe.Element("dirNode", CompassPoint.SouthEast);
        scribe.Element("nullNode", (string)null);
        scribe.EndElement();

        scribe.StartElement("valueList");
        scribe.StartElement("val");
        scribe.Value("stringValue");
        scribe.EndElement();
        scribe.StartElement("valFloat");
        scribe.Value(99.5f);
        scribe.EndElement();
        scribe.StartElement("valBool");
        scribe.Value(true);
        scribe.EndElement();
        scribe.StartElement("valColor");
        scribe.Value(Color.Red);
        scribe.EndElement();
        scribe.StartElement("valDir");
        scribe.Value(CompassPoint.East);
        scribe.EndElement();
        scribe.EndElement();

        scribe.EndElement();
      }

      var doc = new XmlDocument();
      doc.Load(tempFile);
      var root = new XmlElementReader(doc.DocumentElement);

      root.HasName("ROOT").ShouldBeTrue();
      root.HasName("other").ShouldBeFalse();
      root.Attribute("version").ToFloat().ShouldBe(2.5f);
      root.Attribute("active").ToBool().ShouldBeTrue();
      root.Attribute("inactive").ToBool().ShouldBeFalse();
      root.Attribute("dir").ToCompassPoint(CompassPoint.North).ShouldBe(CompassPoint.NorthWest);
      root.Attribute("title").Text.ShouldBe("Test Title");

      var child = root["child"];
      child.Attribute("id").ToInt().ShouldBe(42);
      child["textNode"].Text.ShouldBe("Hello World");
      child["textNode"].ToText("default").ShouldBe("Hello World");
      child["numberNode"].ToFloat().ShouldBe(123.45f);
      child["flagTrue"].ToBool().ShouldBeTrue();
      child["flagFalse"].ToBool().ShouldBeFalse();
      child["dirNode"].ToCompassPoint(CompassPoint.North).ShouldBe(CompassPoint.SouthEast);
      child["nullNode"].Text.ShouldBe(string.Empty);
      child["nullNode"].ToText("fallback").ShouldBe("fallback");

      var children = root.Children;
      children.Count.ShouldBe(2);
      children[0].Name.ShouldBe("child");
      children[1].Name.ShouldBe("valueList");

      var valList = root["valueList"];
      valList["val"].Text.ShouldBe("stringValue");
      valList["valFloat"].ToFloat().ShouldBe(99.5f);
      valList["valBool"].ToBool().ShouldBeTrue();
      valList["valDir"].ToCompassPoint(CompassPoint.North).ShouldBe(CompassPoint.East);
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }

  [Test]
  public void XmlElementReader_MissingElementsAndAttributes_ReturnGracefulDefaults()
  {
    var doc = new XmlDocument();
    doc.LoadXml("<root><item id=\"bad-num\" flag=\"maybe\" dir=\"invalid-dir\"/></root>");
    var root = new XmlElementReader(doc.DocumentElement);

    var missing = root["nonExistent"]["subChild"];
    missing.Element.ShouldBeNull();
    missing.Name.ShouldBe(string.Empty);
    missing.Text.ShouldBe(string.Empty);
    missing.ToText("default").ShouldBe("default");
    missing.ToInt(10).ShouldBe(10);
    missing.ToFloat(3.14f).ShouldBe(3.14f);
    missing.ToBool(true).ShouldBeTrue();
    missing.ToColor(Color.Green).ShouldBe(Color.Green);
    missing.ToCompassPoint(CompassPoint.South).ShouldBe(CompassPoint.South);
    missing.Children.ShouldBeEmpty();

    var item = root["item"];
    item.Attribute("id").ToInt(99).ShouldBe(99);
    item.Attribute("id").ToFloat(1.5f).ShouldBe(1.5f);
    item.Attribute("flag").ToBool(true).ShouldBeTrue();
    item.Attribute("flag").ToBool(false).ShouldBeFalse();
    item.Attribute("dir").ToCompassPoint(CompassPoint.West).ShouldBe(CompassPoint.West);
    item.Attribute("color").ToColor(Color.Purple).ShouldBe(Color.Purple);

    var missingAttr = item.Attribute("nonExistent");
    missingAttr.Text.ShouldBe(string.Empty);
    missingAttr.ToInt(5).ShouldBe(5);
    missingAttr.ToFloat(2.5f).ShouldBe(2.5f);
    missingAttr.ToBool(true).ShouldBeTrue();
    missingAttr.ToColor(Color.Yellow).ShouldBe(Color.Yellow);
    missingAttr.ToCompassPoint(CompassPoint.NorthEast).ShouldBe(CompassPoint.NorthEast);
  }

  [Test]
  public void XmlAttributeReader_ParsesValidAndInvalidValuesCorrectly()
  {
    new XmlAttributeReader("123").ToInt().ShouldBe(123);
    new XmlAttributeReader("abc").ToInt(456).ShouldBe(456);
    new XmlAttributeReader("12.75").ToFloat().ShouldBe(12.75f);
    new XmlAttributeReader("xyz").ToFloat(9.5f).ShouldBe(9.5f);

    new XmlAttributeReader("yes").ToBool().ShouldBeTrue();
    new XmlAttributeReader("YES").ToBool().ShouldBeTrue();
    new XmlAttributeReader("no").ToBool(true).ShouldBeFalse();
    new XmlAttributeReader("NO").ToBool(true).ShouldBeFalse();
    new XmlAttributeReader("invalid").ToBool(true).ShouldBeTrue();
    new XmlAttributeReader("invalid").ToBool(false).ShouldBeFalse();

    new XmlAttributeReader("n").ToCompassPoint(CompassPoint.South).ShouldBe(CompassPoint.North);
    new XmlAttributeReader("S").ToCompassPoint(CompassPoint.North).ShouldBe(CompassPoint.South);
    new XmlAttributeReader("invalid").ToCompassPoint(CompassPoint.East).ShouldBe(CompassPoint.East);

    new XmlAttributeReader("#FF0000").ToColor(Color.Black).ToArgb().ShouldBe(Color.Red.ToArgb());
    new XmlAttributeReader("invalid-color").ToColor(Color.Blue).ShouldBe(Color.Blue);
  }
}
