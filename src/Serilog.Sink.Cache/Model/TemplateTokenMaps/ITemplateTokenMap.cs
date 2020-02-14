using Serilog.Parsing;

namespace Serilog.Sink.Cache.Model.TemplateTokenMaps
{
    public interface ITemplateTokenMap
    {
        MessageTemplateToken ToMessageTemplateToken();
    }
}