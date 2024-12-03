using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class Helicopter: Body
    {
        Collision collision = new Collision(2);
        public bool ctrlW, ctrlS, ctrlA, ctrlD, ctrlSpace, ctrlShift, ctrlQ, ctrlE, ctrlB;
        public int Id;
        public string Name = "heli";
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

        public Helicopter(Helicopter boat) : base(boat)
        {
            model = boat.model;
            posErrors = new Vector3[verlets.Length];
        }
        public Helicopter(GraphicsDevice dev, Model model)
        {
            this.model = model;

            float size = 1f;
            var rng = new Random();
            var pos = new Vector3((float)rng.NextDouble() * 20, 5, (float)rng.NextDouble() * 20);

            verlets = new Verlet[]
            {
                new Verlet( pos + new Vector3( size, 0, -size ) ), // Front-right
                new Verlet( pos + new Vector3( size, 0, size ) ),  // Back-right
                new Verlet( pos + new Vector3( -size, 0, size ) ), // Back-left
                new Verlet( pos + new Vector3( -size, 0, -size ) ),// Front-left
                new Verlet( pos + new Vector3( 0, 2, 0 ) )         // Center (rotor)
            };

            GenerateFullyConnectedBody();
        }
        public void SetPosition(Vector3 newPosition)
        {
            Vector3 currentPosition = Position;
            Vector3 offset = newPosition - currentPosition;

            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].Pos += offset;
                verlets[i].pPos += offset;
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
            // Gravity Force
            Vector3 gravity = new Vector3(0, -9.81f, 0);
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = gravity;

            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);

 
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
            if (ctrlB) // Or any other condition to reset orientation
            {
                verlets[4].Acc = Vector3.Up;
                verlets[4].Acc = d;
            }
            for (int i = 0; i < verlets.Length; i++)
            {
                var verlet = verlets[i];

                // Helicopter lift (space to ascend, shift to descend)
                if (ctrlSpace)
                    verlet.Acc += Vector3.Up * 20f; // Ascend
                if (ctrlShift)
                    verlet.Acc -= Vector3.Up * 10f; // Descend

                // Forward/backward movement (W/S)
                if (ctrlW)
                    verlet.Acc += d * 5f; // Move forward
                if (ctrlS)
                    verlet.Acc -= d * 5f; // Move backward

                // Strafing (A/D)
                if (ctrlA)
                    verlet.Acc -= r * 5f; // Move left
                if (ctrlD)
                    verlet.Acc += r * 5f; // Move right
                verlets[i] = verlet;
            }
            // Yaw (Q/E) - rotation around vertical axis (Up)
            
            if (ctrlQ)// Rotate left
            {
                verlets[0].Acc -= r * 1f;
                verlets[1].Acc -= r * 1f;
                verlets[2].Acc += r;
                verlets[3].Acc += r;
            }
            
            if (ctrlE)// Rotate right
            {
                verlets[0].Acc += r * 1f;
                verlets[1].Acc += r * 1f;
                verlets[2].Acc -= r * 1f;
                verlets[3].Acc -= r * 1f;
            }
        }
   

        public static void Collide(Helicopter b1, Helicopter b2)
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
