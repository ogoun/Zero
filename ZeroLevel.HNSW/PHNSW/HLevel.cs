using System;

namespace ZeroLevel.HNSW.PHNSW
{
    internal class HLevel<TPayload>
        : IPHNSWLevel<TPayload>
    {
        private readonly float _distance;
        public HLevel(float distance)
        {
            _distance = distance;
        }

        public Node<TPayload> Node { get; set; } = null;
        public IPHNSWLevel<TPayload> NextLevelA { get; set; }
        public IPHNSWLevel<TPayload> NextLevelB { get; set; }

        private float _abDistance = float.MinValue;

        public void Add(Node<TPayload> node)
        {
            if (NextLevelA.Node == null) { NextLevelA.Node = node; }
            else if (NextLevelB.Node == null)
            {
                NextLevelB.Node = node;
                _abDistance = PHNSWMetric.CosineDistance(NextLevelA.Node.Vector, NextLevelB.Node.Vector);
            }
            else
            {
                var an = PHNSWMetric.CosineDistance(NextLevelA.Node.Vector, node.Vector);
                var bn = PHNSWMetric.CosineDistance(NextLevelB.Node.Vector, node.Vector);

                var abDiff = Math.Abs(_distance - _abDistance);
                var anDiff = Math.Abs(_distance - an);
                var bnDiff = Math.Abs(_distance - bn);

                if (abDiff < anDiff && abDiff < bnDiff)
                {
                    if (an < bn)
                    {
                        NextLevelA.Add(node);
                    }
                    else
                    {
                        NextLevelB.Add(node);
                    }
                }
                else if (anDiff < bnDiff && anDiff < abDiff)
                {
                    NextLevelA.Node = node;
                    NextLevelA.Add(node);
                }
                else
                {
                    NextLevelB.Node = node;
                    NextLevelB.Add(node);
                }
            }
        }
    }
}
