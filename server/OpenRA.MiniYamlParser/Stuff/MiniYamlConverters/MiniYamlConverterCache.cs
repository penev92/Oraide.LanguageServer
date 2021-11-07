using System;
using System.Collections.Concurrent;

namespace OpenRA.MiniYamlParser.Stuff.MiniYamlConverters
{
    class MiniYamlConverterCache
    {
        readonly ConcurrentDictionary<Type, MiniYamlConverter> converters = new ConcurrentDictionary<Type, MiniYamlConverter>();

        public MiniYamlConverter GetConverter(Type type)
        {
            if (converters.TryGetValue(type, out var converter))
                return converter;

            return null;
        }
    }
}
