using System.Collections.Generic;
using System.Diagnostics;

namespace OpenRA.MiniYamlParser.Stuff
{
    public struct ReadStack
    {
        public ReadStackFrame Current;

        private List<ReadStackFrame> _previous;
        public int _index;

        public void Push()
        {
            if (_previous == null)
            {
                _previous = new List<ReadStackFrame>();
            }

            if (_index == _previous.Count)
            {
                // Need to allocate a new array element.
                _previous.Add(Current);
            }
            else
            {
                Debug.Assert(_index < _previous.Count);

                // Use a previously allocated slot.
                _previous[_index] = Current;
            }

            Current.Reset();
            _index++;
        }

        public void Pop()
        {
            Debug.Assert(_index > 0);
            Current = _previous[--_index];
        }
    }
}
