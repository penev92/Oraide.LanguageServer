using System;

namespace OpenRA.MiniYamlParser.Stuff
{
    public enum MiniYamlToken
    {
        None,
        Key,
        String,
        Comment,
    }

    public ref struct MiniYamlReader
    {
        private readonly ReadOnlySpan<char> yaml;

        public ReadOnlySpan<char> Value { get; set; }
        public MiniYamlToken CurrentToken;
        private int consumed;

        public MiniYamlReader(ReadOnlySpan<char> yaml)
        {
            CurrentToken = MiniYamlToken.None;
            Value = ReadOnlySpan<char>.Empty;
            this.yaml = yaml;
            consumed = 0;
        }

        public bool Read()
        {
            

            while (true)
            {
                var c = yaml[consumed];
                switch (c)
                {

                    case ' ':
                    case '\n':
                        SkipWhitespace();
                        break;
                    case ':':
                        consumed++;
                        break;
                    case '#':
                        CurrentToken = MiniYamlToken.Comment;
                        SkipComment();
                        break;
                    default:
                    {
                        if (CurrentToken == MiniYamlToken.Key)
                            return ConsumeValue();
                        else
                        {
                            return ConsumeKey();
                        }
                    }

                }
            }


            if (yaml[consumed] == '#')
            {
                SkipComment();
            }

            if (CurrentToken == MiniYamlToken.None)
                return ConsumeKey();
            if (CurrentToken == MiniYamlToken.Key)
                return ConsumeValue();

            if (CurrentToken == MiniYamlToken.String)
                return ConsumeKey();

            return false;
        }

        private void SkipWhitespace()
        {
            var local = yaml;

            for (; consumed < local.Length; consumed++)
            { 
                if (local[consumed] != ' ' && local[consumed] != '\n')
                    break;
            }
        }

        char NextChar()
        {
            return yaml[consumed++];
        }

        char PeekChar()
        {
            return yaml[consumed];
        }

        private bool ConsumeValue()
        {
            var local = yaml.Slice(consumed);

            var i = 0;
            for (; i < local.Length; i++)
            {

                var c = local[i];

                if (c == '\n' || c == '#')
                {
                    i--;
                    break;

                }

                consumed++;
            }

            Value = local.Slice(0, i);
            CurrentToken = MiniYamlToken.String;

            return true;
        }

        bool ConsumeKey()
        {
            var localBuffer = yaml.Slice(consumed);
            var i = 0;

            for (; i < localBuffer.Length; i++)
            {
                var c = localBuffer[i];

                if (c == ':' || c == '@')
                {
                    break;
                }
            }

            consumed += i;


            Value = localBuffer.Slice(0, i);
            CurrentToken = MiniYamlToken.Key;

            return true;
        }

        void SkipComment()
        {
            var local = yaml.Slice(consumed);
            var i = 0;

            for (; i < local.Length; i++)
            {
                consumed++;
                if (local[i] == '\n')
                    break;

            }
        }
    }
}
