using System;

namespace OpenRA.MiniYamlParser.Stuff.MiniYamlConverters
{
    public abstract class MiniYamlConverter
    {
        public abstract bool CanConvert(Type typeToConvert);

        // This is used internally to quickly determine the type being converted for JsonConverter<T>.
        internal virtual Type TypeToConvert => null;
    }

    public abstract class MiniYamlConverter<T> : MiniYamlConverter
    {
        public abstract T Read(string value);
        public abstract string Write(T value);
    }
}
