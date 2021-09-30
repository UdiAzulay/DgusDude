using System;
using System.Collections;
using System.Collections.Generic;

namespace DgusDude.Core
{
    public class Slicer : IEnumerable<ArraySegment<byte>>
    {
        private ArraySegment<byte> _array;
        private uint _blockSize, _firstBlockSize, _length;
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerator<ArraySegment<byte>> IEnumerable<ArraySegment<byte>>.GetEnumerator() { return GetEnumerator(); }
        public Enumerator GetEnumerator() { return new Enumerator(this); }
        public Slicer(ArraySegment<byte> data, uint blockSize, int startPosition = 0) 
            :this((uint)data.Count, blockSize, startPosition)
        { _array = data; }

        public Slicer(uint length, uint blockSize, int startPosition = 0)
        {
            //_array = Extensions.EmptyArraySegment; 
            _length = length;
            if (blockSize == 0) blockSize = int.MaxValue;
            _blockSize = blockSize;
            _firstBlockSize = blockSize - ((uint)startPosition % blockSize);
        }

        public class Enumerator : IEnumerator<ArraySegment<byte>>
        {
            private Slicer _enum;
            private uint _position, _endPosition, _arrayOffset;
            public Enumerator(Slicer source)
            {
                _enum = source;
                _arrayOffset = (uint)_enum._array.Offset;
                _endPosition = _arrayOffset + _enum._length;
                _position = _arrayOffset - _enum._blockSize;
            }
            public uint CurrentLength =>
                Math.Min(_enum._firstBlockSize > 0 && _position == _arrayOffset ?
                    _enum._firstBlockSize : _enum._blockSize, _endPosition - _position);
            public void Dispose() { }
            public void Reset() => _position = _arrayOffset - _enum._blockSize;
            public bool MoveNext() => (_position += CurrentLength) < _endPosition;
            object IEnumerator.Current => Current;
            public ArraySegment<byte> Current => new ArraySegment<byte>(_enum._array.Array, (int)_position, (int)CurrentLength);
        }
    }
}
