using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class Boat: Body
    {
        Collision collision = new Collision(2);
        public bool ctrlW, ctrlS, ctrlA, ctrlD;
        public int Id;
        public string Name = "raja";
        public Vector3[] posErrors;
        Model model;
        Matrix localTransform = Matrix.CreateScale(0.3f) * Matrix.CreateRotationY(MathF.PI);
        public Vector3 Position => (verlets[0].Pos + verlets[1].Pos +
            verlets[2].Pos + verlets[3].Pos) * 0.25f;
        public Vector3 Direction => verlets[0].Pos - verlets[3].Pos;
        public Vector3 Right => verlets[1].Pos - verlets[0].Pos;
        public Vector3 Up => 2 * verlets[4].Pos - verlets[0].Pos - verlets[2].Pos;
        public Matrix WorldTransform => Matrix.CreateWorld(Position,
            Vector3.Normalize(Direction), Vector3.Normalize(Up));

        public Boat(Boat boat, BoatDto dto) : base(boat)
        {
            model = boat.model;
            posErrors = new Vector3[verlets.Length];
            for (int i = 0; i < dto.VerletPositions.Length; i++)
                verlets[i].Pos = verlets[i].pPos = dto.VerletPositions[i];
        }
        public Boat(GraphicsDevice dev, Model model)
        {
            this.model = model;
            float w = 0.2f, l = 1;
            var rng = new Random();
            var pos = new Vector3((float)rng.NextDouble() * 20, 5, (float)rng.NextDouble() * 20);
            verlets = new Verlet[]
            {
                new Verlet( pos + new Vector3( l, 0, -w ) ),
                new Verlet( pos + new Vector3( l, 0, w ) ),
                new Verlet( pos + new Vector3( -l, 0, w ) ),
                new Verlet( pos + new Vector3( -l, 0, -w ) ),
                new Verlet( pos + new Vector3( 0, 1, 0 ) ),
                new Verlet( pos + new Vector3( -l, -0.8f, 0 ) )
            };
            GenerateFullyConnectedBody();
        }
        public BoatDto PackState()
        {
            return new BoatDto
            {
                Name = Name,
                ctrlW = ctrlW,
                ctrlS = ctrlS,
                ctrlA = ctrlA,
                ctrlD = ctrlD,
                VerletPositions = Array.ConvertAll(verlets, x => x.Pos)
            };
        }

        public void UnpackState(BoatDto obj)
        {
            Name = obj.Name;
            ctrlW = obj.ctrlW;
            ctrlS = obj.ctrlS;
            ctrlA = obj.ctrlA;
            ctrlD = obj.ctrlD;
            for (int i = 0; i < obj.VerletPositions.Length; i++)
            {
                posErrors[i] = obj.VerletPositions[i] - verlets[i].Pos;
            }
        }

        public void Draw(Camera cam, Vector3 sunDir)
        {
            int idx = 0;
            foreach (var mesh in model.Meshes)
            {
                if (idx++ > 3)
                    break;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.LightingEnabled = true;
                    effect.PreferPerPixelLighting = true;
                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight0.Direction = -sunDir;
                    effect.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                    effect.DirectionalLight0.SpecularColor = new Vector3(1, 1, 1);
                    effect.DirectionalLight1.Enabled = true;
                    effect.DirectionalLight1.Direction = Vector3.Down;
                    effect.DirectionalLight1.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
                    effect.World = localTransform * WorldTransform;
                    effect.View = cam.View;
                    effect.Projection = cam.Projection;
                }
                mesh.Draw();
            }
            //model.Draw(localTransform * WorldTransform, cam.View, cam.Projection);
        }
        public void SetPosition(Vector3 newPosition)
        {
            Vector3 currentPosition = Position; // Get the current computed position
            Vector3 offset = newPosition - currentPosition; // Calculate the offset

            // Apply the offset to all `verlets` contributing to the Position
            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].Pos += offset;
                verlets[i].pPos += offset;
            }
        }
        public void Step()
        {
            ApplyForces();
            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].Step();
                if (posErrors != null)
                {
                    verlets[i].Pos += posErrors[i] * 0.01f;
                    verlets[i].pPos += posErrors[i] * 0.01f;
                }
            }
            ApplyConstraints();
            collision.WorldTransform = WorldTransform;
        }

        private void ApplyForces()
        {
            // Gravitacio
            Vector3 g = new Vector3(0, -9.81f, 0);
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = g;

            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);

            // Felhajto ero
            for (int i = 0; i < 4; i++)
            {
                float height = verlets[i].Pos.Y;
                if (height < 0)
                {
                    verlets[i].Acc += Vector3.Up * Math.Min(-height * 50, 30);
                    verlets[i].AddSqFriction(Vector3.Up, 10);
                }
                float fHeight = Math.Max(0.5f - verlets[i].Pos.Y, 0);
                verlets[i].AddSqFriction(d, 0.05f * fHeight);
                verlets[i].AddSqFriction(r, 0.5f * fHeight);
                verlets[i].AddSqFriction(u, 5 * fHeight);
            }

            bool engineUnderWater = verlets[5].Pos.Y < 0;
            if (engineUnderWater)
            {
                if (ctrlW) verlets[5].Acc += d * 30;
                if (ctrlS) verlets[5].Acc -= d * 5;
                if (ctrlA) verlets[5].Acc += r * (verlets[5].Velocity.Length() + 2) * 0.2f;
                if (ctrlD) verlets[5].Acc -= r * (verlets[5].Velocity.Length() + 2) * 0.2f;
            }


        }

        public static void Collide(Boat b1, Boat b2)
        {
            Vector3 normal, point;
            float penetration;
            if (Collision.Detect(b1.collision, b2.collision, out point, out normal, out penetration))
            {
                b1.verlets[4].Pos -= normal * (penetration * 0.01f);
                b2.verlets[4].Pos += normal * (penetration * 0.01f);
                b1.ApplyConstraints();
                b2.ApplyConstraints();
            }
        }

    }
}
