using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Xml;
using Trizbort.Domain.Misc;

namespace Trizbort.Util;

/// <summary>
///   Wrapper around an XmlElement for ease of access.
/// </summary>
public class XmlElementReader
{
  private List<XmlElementReader> _children;

  public XmlElementReader()
  {
  }

  public XmlElementReader(XmlElement element)
  {
    Element = element;
  }

  public XmlElementReader this[string localName] {
    get {
      if (Element != null)
        foreach (var node in Element.ChildNodes)
        {
          if (!(node is XmlElement))
            continue;

          var element = (XmlElement)node;
          if (element.LocalName == localName) return new XmlElementReader(element);
        }

      return new XmlElementReader();
    }
  }

  public List<XmlElementReader> Children {
    get {
      if (_children == null)
      {
        _children = new List<XmlElementReader>();
        if (Element != null)
          foreach (var node in Element.ChildNodes)
          {
            if (!(node is XmlElement))
              continue;

            var element = (XmlElement)node;
            _children.Add(new XmlElementReader(element));
          }
      }

      return _children;
    }
  }

  public string Name {
    get {
      if (Element != null) return Element.LocalName;
      return string.Empty;
    }
  }

  public string Text {
    get {
      if (Element != null) return Element.InnerText;
      return string.Empty;
    }
  }

  public XmlElement Element { get; }

  public bool HasName(string value)
  {
    return StringComparer.InvariantCultureIgnoreCase.Compare(Name, value) == 0;
  }

  public string ToText(string defaultValue)
  {
    var text = Text;
    if (!string.IsNullOrEmpty(text)) return text;
    return defaultValue;
  }

  public int ToInt()
  {
    return ToInt(0);
  }

  public int ToInt(int defaultValue)
  {
    int value;
    if (int.TryParse(Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return value;
    return defaultValue;
  }

  public float ToFloat()
  {
    return ToFloat(0);
  }

  public float ToFloat(float defaultValue)
  {
    float value;
    if (float.TryParse(Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return value;
    return defaultValue;
  }

  public bool ToBool()
  {
    return ToBool(false);
  }

  public bool ToBool(bool defaultValue)
  {
    if (StringComparer.InvariantCultureIgnoreCase.Compare(Text, XmlScribe.Yes) == 0) return true;

    if (StringComparer.InvariantCultureIgnoreCase.Compare(Text, XmlScribe.No) == 0) return false;
    return defaultValue;
  }

  public Color ToColor(Color defaultValue)
  {
    try
    {
      return ColorTranslator.FromHtml(Text);
    }
    catch (Exception)
    {
      return defaultValue;
    }
  }

  public CompassPoint ToCompassPoint(CompassPoint defaultValue)
  {
    CompassPoint point;
    if (CompassPointHelper.FromName(Text, out point)) return point;
    return defaultValue;
  }

  public XmlAttributeReader Attribute(string localName)
  {
    if (Element != null)
      foreach (XmlAttribute attribute in Element.Attributes)
        if (StringComparer.InvariantCultureIgnoreCase.Compare(attribute.LocalName, localName) == 0)
          return new XmlAttributeReader(attribute.Value);

    return new XmlAttributeReader(string.Empty);
  }
}