using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LiteDB;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sink.Cache.Model.TemplateTokenMaps;

namespace Serilog.Sink.Cache.Model
{
    public class TemplateMap
    {
        public string Text { get; set; }

        public IEnumerable<ITemplateTokenMap> TokenMaps { get; set; }

        [BsonIgnore]
        public IEnumerable<MessageTemplateToken> Tokens
        {
            get => TokenMaps?.Select(t => t.ToMessageTemplateToken());
            set => TokenMaps = value?.Select(mtt =>
            {
                switch (mtt)
                {
                    case TextToken textToken: return TextTokenMap.FromTextToken(textToken) as ITemplateTokenMap;
                    case PropertyToken propertyToken: return PropertyTokenMap.FromPropertyToken(propertyToken) as ITemplateTokenMap;
                    default: throw new InvalidEnumArgumentException($"{nameof(MessageTemplateToken)} of type {mtt.GetType().FullName}could not be mapped");
                }
            });
        }

        public TemplateMap()
        {
        }

        public static TemplateMap FromMessageTemplate(MessageTemplate messageTemplate)
        {
            return new TemplateMap
            {
                Text = messageTemplate.Text,
                Tokens = messageTemplate.Tokens
            };
        }

        public MessageTemplate ToMessageTemplate()
        {
            return Text != null ? new MessageTemplate(Text, Tokens) : new MessageTemplate(Tokens);
        }
    }
}