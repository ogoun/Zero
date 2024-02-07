using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroLevel.HNSW.PHNSW
{
    public static class PHNSWBuilder
    {
        public static IPHNSWLevel<TPayload> Build<TPayload>(int levels)
        {
            var distance = 0.33f;
            var root = new HLevel<TPayload>(distance);
            var horizontalLayers = new List<HLevel<TPayload>>(new[] { root });
            for (var i = 0; i < levels; i++)
            {
                distance /= 2.0f;
                var nextList = new List<HLevel<TPayload>>();
                foreach (var layer in horizontalLayers)
                {
                    var a = new HLevel<TPayload>(distance);
                    var b = new HLevel<TPayload>(distance);
                    layer.NextLevelA = a;
                    layer.NextLevelB = b;
                    nextList.Add(a); 
                    nextList.Add(b);
                }
                horizontalLayers = nextList;
            }
            var uwLevel = new UWLevel<TPayload>();

        }
    }
}
