using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal struct Verlet
    {
        public const float dT = 1f / 60;
        public Vector3 Pos, pPos, Acc, Fric;
        public Verlet(Vector3 pos) { Pos = pPos = pos; Velocity = Omega =  Acc = Fric = Vector3.Zero; }
        public Verlet(float x, float y, float z) : this(new Vector3(x, y, z)) { }
        //public Vector3 Velocity => (Pos - pPos) * (1 / dT);
        public Vector3 Velocity;
        public Vector3 Omega;

        public void Step()
        {
            Vector3 dPos = Pos - pPos + 0.5f * dT * dT * Acc;                                                                                                
            Vector3 f = dT * dT * Fric;
            float C = 1;
            if (Vector3.Dot(dPos, dPos + f) < 0)
                C = -dPos.LengthSquared() / Vector3.Dot(dPos, f);
            Vector3 newPos = Pos + dPos + C * f;
            pPos = Pos;
            Pos = newPos;
            Fric = new Vector3();
            Velocity = (Pos - pPos) * (1 / dT);

        }

        public void helistep(Vector3 center)
        {
            var newVel = Velocity + Acc/2 * dT + Vector3.Cross(Omega, Pos - center);
            pPos = Pos;
            Pos += newVel * dT;
            Velocity += Acc * dT;
         }

        public void AddSqFriction(Vector3 fDir, float fC)
        {
            float length = Vector3.Dot(Velocity, fDir);
            Fric -= fDir * length * Math.Abs(length) * fC;
        }

        public static void ApplyLengthConstraint(ref Verlet v1, ref Verlet v2, float length)
        {
            Vector3 dPos = v2.Pos - v1.Pos;
            float dLength = dPos.Length();
            Vector3 correction = (dLength - length) / dLength * 0.5f * dPos;
            v1.Pos += correction;
            v2.Pos -= correction;
        }

    }
}
