﻿namespace MrWhimble.RailwayMaker.Graph
{
    public struct RouteSectionData
    {
        public Node node;
        public BezierCurve curve;
        public bool reverse;

        public bool IsDefault => node == null && curve == null;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is RouteSectionData data)
            {
                if (data.node == this.node && data.curve == this.curve && data.reverse == this.reverse)
                    return true;
            }

            return false;
        }
    }
}