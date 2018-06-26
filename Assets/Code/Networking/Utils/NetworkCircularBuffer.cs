using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Networking.Utils
{
    public class NetworkCircularBuffer
    {
        public static int INTERP_TIME = 200;

        private List<PointAtTime> _buffer;
        private int _size;
        private int _nextIndex;
        private int _startIndex;

        private PointAtTime _tempPoint;

        public NetworkCircularBuffer()
        {
            _buffer = new List<PointAtTime>();

            _size = Mathf.CeilToInt((float)INTERP_TIME / TimeSyncer.STEP_MS) + 1;
            for (int i = 0; i < _size; i++)
            {
                _buffer.Add(new PointAtTime());
            }

            _tempPoint = new PointAtTime();
        }

        public void Push(float x, float y, float time)
        {
            _buffer[_nextIndex].x = x;
            _buffer[_nextIndex].y = y;
            _buffer[_nextIndex].time = time;

            _nextIndex = Increment(_nextIndex);
            if (_nextIndex == _startIndex)
            {
                _startIndex = Increment(_startIndex);
            }
        }

        public PointAtTime Interpolate(float time)
        {
            time -= INTERP_TIME / 1000.0f;
            ClearOlderThanTime(time);

            var secondIndex = Increment(_startIndex);
            var first = _buffer[_startIndex];
            if (first.time >= time || secondIndex == _nextIndex)
            {
                _tempPoint.x = first.x;
                _tempPoint.y = first.y;
            }
            else
            {
                var second = _buffer[secondIndex];

                if((second.time - first.time) == 0)
                    return new PointAtTime();

                var alpha = (time - first.time) / (second.time - first.time);
                _tempPoint.x = first.x + (second.x - first.x) * alpha;
                _tempPoint.y = first.y + (second.y - first.y) * alpha;
            }

            return _tempPoint;
        }

        public void ClearOlderThanTime(float time)
        {
            if (_startIndex == _nextIndex)
                return;
            var secondIndex = Increment(_startIndex);
            while (_buffer[secondIndex].time < time && secondIndex != _nextIndex)
            {
                _startIndex = secondIndex;
                secondIndex = Increment(secondIndex);
            }
        }

        private int Increment(int i)
        {
            return (i + 1) % _size;
        }
    }
}