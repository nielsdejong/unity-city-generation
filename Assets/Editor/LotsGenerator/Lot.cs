using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Editor.LotsGenerator
{
    class Lot
    {
        public List<Vector2> corners;
		private Vector2 center = Vector2.zero;
        public Block parent;

        public Lot(Block parent)
        {
            this.parent = parent;
            corners = new List<Vector2>();
        }

        private void getCenter()
        {
			//the center is not set yet
			if (center == Vector2.zero) {
				center = Vector2.zero;
				foreach (Vector2 corner in corners) {
					center += corner;
				}
				center /= corners.Count;
			} 
        }

        public float getPopulationValue()
        {
            getCenter();
            return CoordinateHelper.worldToPop(center.x, center.y);
        }
    }
}
