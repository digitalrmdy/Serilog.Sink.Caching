using System;
using Newtonsoft.Json.Linq;
using Serilog.Parsing;

namespace Serilog.Sink.Cache.Mapping
{
    internal class MessageTemplateTokenConverter
    {
        private const string RawTextKey = "rawtext";

        public string SerializeMessageTemplateToken(MessageTemplateToken token)
        {
            JObject obj;
            switch (token)
            {
                case TextToken textToken:
                    obj = SerializeTextToken(textToken);
                    break;
                case PropertyToken propertyToken:
                    obj = SerializePropertyToken(propertyToken);
                    break;
                default:
                    throw new ArgumentException($"Type {token.GetType().FullName} can not be serialized.");
            }

            obj.Add(nameof(Type), token.GetType().AssemblyQualifiedName);
            return obj.ToString();
        }

        public MessageTemplateToken DeserializeMessageTemplateToken(string token)
        {
            var obj = JObject.Parse(token);
            var type = Type.GetType((string) obj[nameof(Type)]);

            if (type == typeof(TextToken))
            {
                return DeserializeTextToken(obj);
            }

            if (type == typeof(PropertyToken))
            {
                return DeserializePropertyToken(obj);
            }

            throw new ArgumentException($"Type {type} can not be deserialized.");
        }

        private JObject SerializeTextToken(TextToken token)
        {
            return new JObject
            {
                {nameof(TextToken.StartIndex), token.StartIndex},
                {nameof(TextToken.Text), token.Text}
            };
        }

        private TextToken DeserializeTextToken(JObject obj)
        {
            var text = obj[nameof(TextToken.Text)]?.ToString();
            var index = obj[nameof(TextToken.StartIndex)]?.ToObject<int>();

            return index.HasValue
                ? new TextToken(text, index.Value)
                : new TextToken(text);
        }

        private JObject SerializePropertyToken(PropertyToken token)
        {
            return new JObject
            {
                {nameof(PropertyToken.PropertyName), token.PropertyName},
                {RawTextKey, token.ToString()},
                {nameof(PropertyToken.Format), token.Format},
                {nameof(Alignment.Direction), token.Alignment?.ToString()},
                {nameof(Alignment.Width), token.Alignment?.ToString()},
                {nameof(PropertyToken.Destructuring), token.Destructuring.ToString()},
                {nameof(PropertyToken.StartIndex), token.StartIndex}
            };
        }

        private PropertyToken DeserializePropertyToken(JObject obj)
        {
            var propertyName = obj[nameof(PropertyToken.PropertyName)]?.ToString();
            var rawText = obj[RawTextKey]?.ToString();
            var format = obj[nameof(PropertyToken.Format)]?.ToString();

            var direction = Enum.TryParse(obj[nameof(Alignment.Direction)]?.ToString(), out AlignmentDirection dir) ? (AlignmentDirection?) dir : null;
            var width = obj[nameof(Alignment.Width)]?.ToObject<int?>();
            var alignment = (width == null || direction == null) ? (Alignment?) null : new Alignment(direction.Value, width.Value);

            Enum.TryParse(obj[nameof(PropertyToken.Destructuring)]?.ToString(), out Destructuring destructuring);
            var startIndex = obj[nameof(PropertyToken.StartIndex)]?.ToObject<int>();

            return startIndex.HasValue
                ? new PropertyToken(propertyName, rawText, format, alignment, destructuring, startIndex.Value)
                : new PropertyToken(propertyName, rawText, format, alignment, destructuring);
        }
    }
}