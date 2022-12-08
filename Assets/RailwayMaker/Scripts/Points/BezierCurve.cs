namespace MrWhimble.RailwayMaker
{
    public class BezierCurve
    {
        public AnchorPoint start;
        public AnchorPoint end;
        public ControlPoint controlStart;
        public ControlPoint controlEnd;

        public BezierCurve(AnchorPoint a, ControlPoint b, ControlPoint c, AnchorPoint d)
        {
            start = a;
            end = d;
            controlStart = b;
            controlEnd = c;
        }
    }
}