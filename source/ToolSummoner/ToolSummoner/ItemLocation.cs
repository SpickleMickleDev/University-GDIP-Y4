using System;
using System.Collections.Generic;
using System.Text;

namespace ToolSummoner
{
    public class ItemLocation
    {
        // Separate class to allow for future expansion of location properties (e.g. Multiple stacked items and only taking the top item, having set item shelves for smaller items, etc.)
        public double xCoordinate { get; set; }
        public double yCoordinate { get; set; }

        public ItemLocation(double x, double y)
        {
            xCoordinate = x;
            yCoordinate = y;
        }

        public bool IsDefined()
        {
            return xCoordinate != 0f && yCoordinate != 0f;
        }
    }
}
