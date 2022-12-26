using System.Collections.Generic;

namespace MrWhimble.RailwayMaker.Graph
{
    public class RailwayRoute
    {
        public bool HasRoute => sections != null && sections.Count > 0 && sections[0].curve != null;
        
        public List<RouteSectionData> sections;

        public RailwayRoute()
        {
            sections = new List<RouteSectionData>();
        }

        public void GetSectionDataAtDistance(float distanceFromStart, out RouteSectionData data, out float distance)
        {
            float dist = distanceFromStart;

            for (int i = 0; i < sections.Count; i++)
            {
                RouteSectionData d = sections[i];
                if (dist - d.curve.Length <= 0)
                {
                    data = d;
                    if (data.reverse)
                        distance = dist;
                    else
                        distance = data.curve.Length - dist;
                    distance = data.curve.Length - dist;
                    return;
                }

                dist -= d.curve.Length;
            }

            data = sections[^1];
            if (data.reverse)
                distance = data.curve.Length;
            else
                distance = 0f;
            return;
        }
    }
}