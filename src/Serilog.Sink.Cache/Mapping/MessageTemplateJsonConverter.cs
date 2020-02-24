using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;

namespace Serilog.Sink.Cache.Mapping
{
    internal class MessageTemplateJsonConverter : JsonConverter<MessageTemplate>
    {
        private readonly MessageTemplateTokenConverter _tokenConverter;

        public MessageTemplateJsonConverter()
        {
            _tokenConverter = new MessageTemplateTokenConverter();
        }

        public override void WriteJson(JsonWriter writer, MessageTemplate value, JsonSerializer serializer)
        {
            var dict = new Dictionary<string, object>
            {
                {nameof(MessageTemplate.Text), value.Text},
                {nameof(MessageTemplate.Tokens), value.Tokens.Select(_tokenConverter.SerializeMessageTemplateToken).ToArray()}
            };

            JObject.FromObject(dict).WriteTo(writer);
        }

        public override MessageTemplate ReadJson(JsonReader reader, Type objectType, MessageTemplate existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = JToken.ReadFrom(reader);

            var text = t[nameof(MessageTemplate.Text)]?.ToString();
            var tokens = t[nameof(MessageTemplate.Tokens)]?.ToObject<string[]>()?.Select(_tokenConverter.DeserializeMessageTemplateToken);

            return string.IsNullOrEmpty(text)
                ? new MessageTemplate(tokens)
                : new MessageTemplate(text, tokens);
        }
    }
}