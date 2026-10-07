using System;
using System.Drawing;
using System.Globalization;
using Trizbort.Domain.Misc;

namespace Trizbort.Util
{
    /// <summary>
    /// Wrapper around an XML attribute for ease of access.
    /// </summary>
    public class XmlAttributeReader
    {
        public XmlAttributeReader(string value)
        {
            Text = value;
        }

        public string Text
        {
            get;
            private set;
        }

        public int ToInt()
        {
            return ToInt(0);
        }

        public int ToInt(int defaultValue)
        {
            int value;
            if (int.TryParse(Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return defaultValue;
        }

        public float ToFloat()
        {
            return ToFloat(0);
        }

        public float ToFloat(float defaultValue)
        {
            float value;
            if (float.TryParse(Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return defaultValue;
        }

        public bool ToBool()
        {
            return ToBool(false);
        }

        public bool ToBool(bool defaultValue)
        {
            if (StringComparer.InvariantCultureIgnoreCase.Compare(Text, XmlScribe.Yes) == 0)
            {
                return true;
            }
            else if (StringComparer.InvariantCultureIgnoreCase.Compare(Text, XmlScribe.No) == 0)
            {
                return false;
            }
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
            if (CompassPointHelper.FromName(Text, out point))
            {
                return point;
            }
            return defaultValue;
        }
    }
}
