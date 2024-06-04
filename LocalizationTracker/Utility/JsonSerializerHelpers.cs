using Kingmaker.Localization.Shared;
using System;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Linq;
using System.Text.Unicode;
using System.Runtime.Serialization.Json;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LocalizationTracker.Utility
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class JsonDefaultValueAttribute : Attribute
    {
        public object? Value;
        public JsonDefaultValueAttribute(object? value)
        {
            Value = value;
        }
    }

    public static partial class JsonSerializerHelpers
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions;

        static JsonSerializerHelpers()
        {
            /*var encoderSettings = new TextEncoderSettings(UnicodeRanges.All);
            encoderSettings.AllowCharacter('\u00a0');
            var jsEncType = typeof(JavaScriptEncoder).Assembly.GetType("System.Text.Encodings.Web.DefaultJavaScriptEncoder");
            var ctor = jsEncType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(TextEncoderSettings), typeof(bool) });
            var enc = (JavaScriptEncoder)ctor.Invoke(new object[] { encoderSettings, true });*/

            JsonSerializerOptions = new()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new LocaleJsonConverter()
                },
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { RemoveEmptyStrings }
                }
            };
        }
            
        private class LocaleJsonConverter : JsonConverter<Locale>
        {
            public override Locale Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                /*if (reader.TokenType == JsonTokenType.Number)
                {
                    var localeIndex = reader.GetInt32();
                    return (Locale)localeIndex;
                }*/

                var str = reader.GetString();
                return str is null ? Locale.Empty : new Locale(str);
            }

            public override void Write(Utf8JsonWriter writer, Locale locale, JsonSerializerOptions options) =>
                    writer.WriteStringValue(locale.Code);
        }

        private static void RemoveEmptyStrings(JsonTypeInfo typeInfo)
        {
            foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
            {
                object[] ignoreAtributes = propertyInfo.AttributeProvider?.GetCustomAttributes(typeof(JsonIgnoreAttribute), true) ?? Array.Empty<object>();
                object[] defaultValueAttributes = propertyInfo.AttributeProvider?.GetCustomAttributes(typeof(JsonDefaultValueAttribute), true) ?? Array.Empty<object>();

                if (ignoreAtributes.FirstOrDefault() is not JsonIgnoreAttribute ignoreAttribute || ignoreAttribute.Condition != JsonIgnoreCondition.WhenWritingDefault)
                    continue;

                if (defaultValueAttributes.FirstOrDefault() is not JsonDefaultValueAttribute defaultValueAttribute)
                    continue;

                if (propertyInfo.Get is Func<object, object?> getProperty)
                {
                    propertyInfo.Get = (obj) =>
                    {
                        var value = getProperty(obj);
                        if (value != null && value.Equals(defaultValueAttribute.Value))
                        {
                            return default;
                        }

                        return value;
                    };
                }
            }
        }
        
        [GeneratedRegex("\\\\u(?<Value>[a-fA-F0-9]{4})", RegexOptions.Compiled)]
        private static partial Regex UnescapeUnicodeSymbolsRegex();
        public static string UnescapeUnicodeSymbols(string value)
        {
            return UnescapeUnicodeSymbolsRegex().Replace(
                value,
                m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString()
            );
        }
    }
}
