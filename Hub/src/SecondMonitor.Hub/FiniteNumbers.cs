using System.Text.Json;
using System.Text.Json.Serialization;

namespace SecondMonitor.Hub;

/// <summary>
/// Un calcul impossible (division par zéro sur des données de jeu inhabituelles) donne NaN ou l'infini.
/// Écrits « "NaN" » dans le JSON, ils arrivaient à l'interface comme du texte et la faisaient planter (écran noir).
/// On les écrit « null » : l'interface affiche alors « — ».
/// </summary>
public static class FiniteNumbers
{
    public static void Register(JsonSerializerOptions options)
    {
        options.Converters.Add(new DoubleConverter());
        options.Converters.Add(new NullableDoubleConverter());
        options.Converters.Add(new FloatConverter());
    }

    private static double ReadDouble(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return double.NaN;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return double.TryParse(reader.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : double.NaN;
        }

        return reader.GetDouble();
    }

    private sealed class DoubleConverter : JsonConverter<double>
    {
        public override bool HandleNull => true;

        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            double v = ReadDouble(ref reader);
            return double.IsNaN(v) ? 0 : v;
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsFinite(value))
            {
                writer.WriteNumberValue(value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    private sealed class NullableDoubleConverter : JsonConverter<double?>
    {
        public override bool HandleNull => true;

        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            double v = ReadDouble(ref reader);
            return double.IsNaN(v) ? null : v;
        }

        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value is double v && double.IsFinite(v))
            {
                writer.WriteNumberValue(v);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    private sealed class FloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => (float)ReadDouble(ref reader);

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsFinite(value))
            {
                writer.WriteNumberValue(value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
