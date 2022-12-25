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
    }
}