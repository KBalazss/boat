using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class Collision
    {
        public float BoundingR;
        public Matrix WorldTransform = Matrix.Identity;
        public Collision(float boundingSphere) { BoundingR = boundingSphere; }

        public static bool Detect(Collision coll1, Collision coll2,
            out Vector3 cPoint, out Vector3 cNormal, out float cPenetration)
        {
            Vector3 pos1 = Vector3.Transform(Vector3.Zero, coll1.WorldTransform);
            Vector3 pos2 = Vector3.Transform(Vector3.Zero, coll2.WorldTransform);
            Vector3 v = pos2 - pos1;
            float sd = v.LengthSquared();
            float sumR = coll1.BoundingR + coll2.BoundingR;
            if (sd < sumR * sumR)
            {
                float d = (float)Math.Sqrt(sd);
                cPenetration = sumR - d;
                cNormal = v * (1 / d);
                cPoint = Vector3.Lerp(pos1, pos2, coll1.BoundingR / sumR);
                return true;
            }
            else
            {
                cPoint = cNormal = Vector3.Zero;
                cPenetration = 0;
                return false;
            }
        }
    }
}
