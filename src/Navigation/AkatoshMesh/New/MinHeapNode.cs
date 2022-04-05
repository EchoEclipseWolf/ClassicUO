﻿using System.Collections.Generic;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;

namespace RoyT.AStar
{
    /// <summary>
    /// Node in a heap
    /// </summary>
    internal sealed class MinHeapNode
    {        
        public MinHeapNode(Node node, MinHeapNode previous, double expectedCost)
        {
            this.Node = node;
            this.Previous = previous;
            this.ExpectedCost = expectedCost;            
        }

        public Node Node { get; }
        public MinHeapNode Previous { get; }
        public Point3D Position => Node.Location;
        public HashSet<int> LinkedNodes => Node.LinkedIds;
        public int PositionHash => Node.PositionHash;
        public double ExpectedCost { get; set; }                
        public MinHeapNode Next { get; set; }
    }
}
