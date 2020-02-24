using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Serilog.Sink.Cache.Mapping
{
    public static class MapperConfig
    {
        public static void Configure()
        {
            ConfigureSettings();

            ConfigureObjectJsonMapper();
            ConfigureObjectBsonMapper();
        }


        private static void ConfigureSettings()
        {
            BsonMapper.Global.EmptyStringToNull = false;
            BsonMapper.Global.TrimWhitespace = false;
        }

        private static void ConfigureObjectBsonMapper()
        {
            BsonMapper.Global.RegisterType(
                logEvent => new BsonValue(JsonConvert.SerializeObject(logEvent)),
                bson => JsonConvert.DeserializeObject<LogEvent>(bson.AsString)
            );
        }


        private static void ConfigureObjectJsonMapper()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new MessageTemplateJsonConverter(),
                    new LogEventJsonConverter()
                },
                DateParseHandling = DateParseHandling.DateTimeOffset,
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore
            };
        }
    }
}