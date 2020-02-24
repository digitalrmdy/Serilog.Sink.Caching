using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Serilog.Events;

namespace Serilog.Sink.Cache.Mapping
{
    internal class LogEventPropertyConverter
    {
        private const string ScalarValueTypeKey = nameof(ScalarValueTypeKey);

        private const string DictionaryKeyKey = nameof(DictionaryKeyKey);
        private const string DictionaryValueKey = nameof(DictionaryValueKey);
        private const string DictionaryValueTypeKey = nameof(DictionaryValueTypeKey);

        private const string SequenceValueKey = nameof(SequenceValueKey);
        private const string SequenceValueTypeKey = nameof(SequenceValueTypeKey);

        public string SerializeLogEventProperty(LogEventProperty property)
        {
            return LogEventPropertyToJObject(property).ToString();
        }

        public LogEventProperty DeserializeLogEventProperty(string json)
        {
            return JObjectToLogEventProperty(JObject.Parse(json));
        }

        private JObject LogEventPropertyToJObject(LogEventProperty property)
        {
            var jsonValue = LogEventPropertyValueToJObject(property.Value);

            jsonValue.Add(nameof(LogEventProperty.Name), property.Name);
            jsonValue.Add(nameof(Type), property.Value.GetType().AssemblyQualifiedName);

            return jsonValue;
        }

        private LogEventProperty JObjectToLogEventProperty(JObject obj)
        {
            var type = Type.GetType((string) obj[nameof(Type)]);

            var value = JObjectToLogEventPropertyValue(obj, type);
            var name = obj[nameof(LogEventProperty.Name)].ToString();

            return new LogEventProperty(name, value);
        }

        private JObject LogEventPropertyValueToJObject(LogEventPropertyValue value)
        {
            JObject jsonValue;
            switch (value)
            {
                case DictionaryValue dictionaryValue:
                    jsonValue = DictionaryValueToJObject(dictionaryValue);
                    break;
                case ScalarValue scalarValue:
                    jsonValue = ScalarValueToJObject(scalarValue);
                    break;
                case SequenceValue sequenceValue:
                    jsonValue = SequenceValueToJObject(sequenceValue);
                    break;
                case StructureValue structureValue:
                    jsonValue = StructureValueToJObject(structureValue);
                    break;
                default:
                    throw new ArgumentException($"Type {value.GetType().FullName} can not be serialized.");
            }

            return jsonValue;
        }

        private LogEventPropertyValue JObjectToLogEventPropertyValue(JObject obj, Type type)
        {
            if (type == typeof(DictionaryValue))
            {
                return JObjectToDictionaryValue(obj);
            }

            if (type == typeof(ScalarValue))
            {
                return JObjectToScalarValue(obj);
            }

            if (type == typeof(SequenceValue))
            {
                return JObjectToSequenceValue(obj);
            }

            if (type == typeof(StructureValue))
            {
                return JObjectToStructureValue(obj);
            }

            throw new ArgumentException($"Type {type} can not be deserialized.");
        }

        private JObject DictionaryValueToJObject(DictionaryValue dictionaryValue)
        {
            var jArray = new JArray();

            foreach (var kvp in dictionaryValue.Elements)
            {
                var key = ScalarValueToJObject(kvp.Key);
                var value = LogEventPropertyValueToJObject(kvp.Value);
                var jObj = new JObject
                {
                    {DictionaryKeyKey, key},
                    {DictionaryValueKey, value},
                    {DictionaryValueTypeKey, kvp.Value.GetType().AssemblyQualifiedName}
                };
                jArray.Add(jObj);
            }

            return new JObject
            {
                {nameof(DictionaryValue.Elements), jArray}
            };
        }

        private JObject ScalarValueToJObject(ScalarValue value)
        {
            return new JObject
            {
                {nameof(ScalarValueTypeKey), value.Value.GetType().AssemblyQualifiedName},
                {nameof(ScalarValue.Value), JToken.FromObject(value.Value)}
            };
        }

        private JObject SequenceValueToJObject(SequenceValue value)
        {
            var jArray = new JArray();

            foreach (var element in value.Elements)
            {
                var jObj = LogEventPropertyValueToJObject(element);
                var type = element.GetType().AssemblyQualifiedName;

                jArray.Add(new JObject
                {
                    {SequenceValueTypeKey, type},
                    {SequenceValueKey, jObj}
                });
            }

            return new JObject
            {
                {nameof(SequenceValue.Elements), jArray}
            };
        }

        private JObject StructureValueToJObject(StructureValue value)
        {
            var jArray = new JArray();

            foreach (var property in value.Properties)
            {
                jArray.Add(LogEventPropertyToJObject(property));
            }

            return new JObject
            {
                {nameof(StructureValue.TypeTag), value.TypeTag},
                {nameof(StructureValue.Properties), jArray}
            };
        }

        private DictionaryValue JObjectToDictionaryValue(JObject obj)
        {
            var list = obj[nameof(DictionaryValue.Elements)].ToObject<List<JObject>>();
            var dict = new Dictionary<ScalarValue, LogEventPropertyValue>();

            foreach (var jObj in list)
            {
                var key = JObjectToScalarValue((JObject) jObj[DictionaryKeyKey]);
                var type = Type.GetType(jObj[DictionaryValueTypeKey].ToString());
                var value = JObjectToLogEventPropertyValue(new JObject(jObj[DictionaryValueKey]), type);

                dict.Add(key, value);
            }

            return new DictionaryValue(dict);
        }

        private ScalarValue JObjectToScalarValue(JObject obj)
        {
            var type = Type.GetType(obj[ScalarValueTypeKey].ToString());
            var value = obj[nameof(ScalarValue.Value)].ToObject(type);

            return new ScalarValue(value);
        }

        private SequenceValue JObjectToSequenceValue(JObject obj)
        {
            var jArray = obj[nameof(SequenceValue.Elements)].ToObject<List<JObject>>();
            var list = new List<LogEventPropertyValue>();

            foreach (var jObj in jArray)
            {
                var type = Type.GetType(jObj[SequenceValueTypeKey].ToString());
                var val = JObjectToLogEventPropertyValue((JObject) jObj[SequenceValueKey], type);
                list.Add(val);
            }

            return new SequenceValue(list);
        }

        private StructureValue JObjectToStructureValue(JObject obj)
        {
            var typeTag = obj[nameof(StructureValue.TypeTag)].ToString();
            var jArray = obj[nameof(SequenceValue.Elements)].ToObject<List<JObject>>();
            var list = new List<LogEventProperty>();

            foreach (var jObj in jArray)
            {
                list.Add(JObjectToLogEventProperty(jObj));
            }

            return new StructureValue(list, typeTag);
        }
    }
}