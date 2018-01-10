//////////////////////////////////////////////
// Fork by : Derek Tremblay (derektremblay666@gmail.com)
// Original code from : https://stackoverflow.com/questions/11447529/convert-an-object-to-an-xml-string
//////////////////////////////////////////////

using System.IO;
using System.Xml.Serialization;

namespace WpfHexaEditor.Core
{
    public class SerializerHelper
    {
        /// <summary>
        /// Serialize an object
        /// </summary>
        public static string Serialize<T>(T dataToSerialize)
        {
            if (dataToSerialize == null) return null;

            using (var stringwriter = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(stringwriter, dataToSerialize);
                return stringwriter.ToString();
            }
        }

        /// <summary>
        /// Deserialize an object
        /// </summary>
        public static T Deserialize<T>(string xmlText)
        {
            if (string.IsNullOrWhiteSpace(xmlText)) return default;

            using (var stringReader = new StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T) serializer.Deserialize(stringReader);
            }
        }
    }
}
