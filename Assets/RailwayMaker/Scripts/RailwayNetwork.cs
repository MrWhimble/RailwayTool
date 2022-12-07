using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class RailwayNetwork
    {
        
        public List<Point> points;

        public RailwayNetwork()
        {
            points = new List<Point>();
            
            points.Add(new AnchorPoint());
            points.Add(new ControlPoint());
            points.Add(new ControlPoint());
            points.Add(new AnchorPoint());

            ((ControlPoint)points[2]).flipped = true;
            
            ((AnchorPoint)points[0]).AddControlPoint((ControlPoint)points[1]);
            ((AnchorPoint)points[3]).AddControlPoint((ControlPoint)points[2]);
            
            ((AnchorPoint)points[0]).UpdatePosition(new Vector3(0, 0, 0));
            ((AnchorPoint)points[3]).UpdatePosition(new Vector3(4, 0, 0));
            
            ((ControlPoint)points[1]).UpdatePosition(new Vector3(1, 1, 0));
            ((ControlPoint)points[2]).UpdatePosition(new Vector3(3, -1, 0));
        }
    }
}