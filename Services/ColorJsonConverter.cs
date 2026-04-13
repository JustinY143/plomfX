using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace plomfX.Services
{
    /// <summary>
    /// JSON converter for System.Windows.Media.Color that serializes to/from hex string (#AARRGGBB).
    /// </summary>
    public class ColorJsonConverter : JsonConverter<System.Windows.Media.Color>
    {
        public override System.Windows.Media.Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? hex = reader.GetString();
            if (string.IsNullOrEmpty(hex))
                return System.Windows.Media.Colors.White;

            try
            {
                // Use WPF's ColorConverter to parse hex string
                object? converted = System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return converted is System.Windows.Media.Color color ? color : System.Windows.Media.Colors.White;
            }
            catch
            {
                return System.Windows.Media.Colors.White;
            }
        }

        public override void Write(Utf8JsonWriter writer, System.Windows.Media.Color value, JsonSerializerOptions options)
        {
            // Serialize as #AARRGGBB string
            writer.WriteStringValue(value.ToString());
        }
    }
}