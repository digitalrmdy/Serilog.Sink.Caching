using Serilog.Parsing;

namespace Serilog.Sink.Cache.Model.TemplateTokenMaps
{
    public class TextTokenMap : ITemplateTokenMap
    {
        public int StartIndex { get; set; }
        public string Text { get; set; }

        public TextTokenMap()
        {
        }


        public MessageTemplateToken ToMessageTemplateToken()
        {
            return new TextToken(Text, StartIndex);
        }

        public static TextTokenMap FromTextToken(TextToken textToken)
        {
            return new TextTokenMap
            {
                Text = textToken.Text,

                StartIndex = textToken.StartIndex
            };
        }
    }
}