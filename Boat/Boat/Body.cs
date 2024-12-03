using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class Body
    {
        public Verlet[] verlets;
        protected int[] cPairs;
        protected float[] cLengths;
        protected bool cPrevDir = false;
        protected int cCycles = 10;

        public Body() { }
        public Body(Body rb)
        {
            verlets = (Verlet[])rb.verlets.Clone();
            cPairs = (int[])rb.cPairs.Clone();
            cLengths = (float[])rb.cLengths.Clone();
            cCycles = rb.cCycles;
        }

        public void GenerateFullyConnectedBody()
        {
            cLengths = new float[(verlets.Length - 1) * verlets.Length / 2];
            cPairs = new int[cLengths.Length * 2];
            int idx = 0;
            for (int i = 0; i < verlets.Length - 1; i++)
                for (int j = i + 1; j < verlets.Length; j++, idx++)
                {
                    cPairs[2 * idx] = i;
                    cPairs[2 * idx + 1] = j;
                    cLengths[idx] = (verlets[i].Pos - verlets[j].Pos).Length();
                }
        }

        public void ApplyConstraints()
        {
            int i = cPrevDir ? cLengths.Length - 1 : 0;
            int dir = cPrevDir ? -1 : 1;
            for (int cycle = 0; cycle < cCycles; cycle++)
            {
                for (; i < cLengths.Length && i >= 0; i += dir)
                    Verlet.ApplyLengthConstraint(ref verlets[cPairs[2 * i]],
                    ref verlets[cPairs[2 * i + 1]], cLengths[i]);
                dir = -dir;
                i += dir;
            }
            cPrevDir = !cPrevDir;
        }
    }
}
