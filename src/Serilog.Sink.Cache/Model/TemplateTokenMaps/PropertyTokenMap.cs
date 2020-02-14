using Serilog.Parsing;

namespace Serilog.Sink.Cache.Model.TemplateTokenMaps
{
    public class PropertyTokenMap : ITemplateTokenMap
    {
        public int StartIndex { get; set; }
        public string PropertyName { get; set; }
        public Destructuring Destructuring { get; set; }
        public string Format { get; set; }

        public AlignmentDirection? AlignmentDirection { get; set; }
        public int? Width { get; set; }
        public string RawText { get; set; }

        public PropertyTokenMap()
        {
        }

        public MessageTemplateToken ToMessageTemplateToken()
        {
            Alignment? alignment = null;

            if (AlignmentDirection.HasValue && Width.HasValue)
            {
                alignment = new Alignment(AlignmentDirection.Value, Width.Value);
            }
            
            return new PropertyToken(PropertyName, RawText, Format, alignment, Destructuring, StartIndex);
        }

        public static PropertyTokenMap FromPropertyToken(PropertyToken propertyToken)
        {
            return new PropertyTokenMap()
            {
                StartIndex = propertyToken.StartIndex,
                PropertyName = propertyToken.PropertyName,
                Destructuring = propertyToken.Destructuring,
                Format = propertyToken.Format,
                AlignmentDirection = propertyToken.Alignment?.Direction,
                Width = propertyToken.Alignment?.Width,
                RawText = propertyToken.ToString()
            };
        }
    }
}