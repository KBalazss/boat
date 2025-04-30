using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Vector3 Right => verlets[0].Pos - verlets[3].Pos;
        public Vector3 Direction => verlets[0].Pos - verlets[1].Pos;
        public Vector3 Up => Vector3.Cross(Right,Direction);
        public Matrix WorldTransform => Matrix.CreateWorld(Position,
            Vector3.Normalize(Direction), Vector3.Normalize(Up));
        private float rotorAngle = 0f;
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
                new Verlet( pos + new Vector3( 0, 0, 0 ) ),        // Center (rotor)
                //new Verlet( pos + new Vector3(0,0.2f, 0) ),
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
            Matrix rotorRotation = Matrix.CreateRotationY(MathHelper.ToRadians(rotorAngle));
            foreach (var mesh in model.Meshes)
            {
                Matrix worldMatrix = localTransform * WorldTransform;
                if (mesh.Name == "static_rotor")
                {
                    worldMatrix = rotorRotation * localTransform * WorldTransform;
                    //foreach (ModelMeshPart part in mesh.MeshParts)
                    //{
                    //    part.Effect.Parameters["World"].SetValue(rotorRotation);
                    //}         
                }
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
                    effect.World = worldMatrix;           
                    effect.View = cam.View;
                    effect.Projection = cam.Projection;
                }
                mesh.Draw();
            }
            //model.Draw(localTransform * WorldTransform, cam.View, cam.Projection);
        }
        public void Step(GameTime gameTime)
        {
            float rotationSpeed = 1500f; // Degrees per second
            rotorAngle += rotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            rotorAngle %= 360f; // Keep the angle within 0-360 degrees
            ApplyForces();
            for (int i = 0; i < verlets.Length; i++)
            {
                verlets[i].helistep(verlets[4].Pos);
            }
            ApplyConstraints();
            collision.WorldTransform = WorldTransform;
        }

        private void ApplyForces()
        {

             //Gravity Force
            Vector3 gravity = new Vector3(0, -9.81f, 0);
            for (int i = 0; i < verlets.Length; i++)
                verlets[i].Acc = gravity;

            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);

            if (ctrlB) // Or any other condition to reset orientation
            {
                verlets[4].Acc = Vector3.Up;
                verlets[4].Acc = d;
            }
            

            for (int i = 0; i < verlets.Length; i++)
            {

                var verlet = verlets[i];
                verlet.Acc = Vector3.Zero;
                verlet.Omega = Vector3.Zero;
                verlet.Acc = gravity;
                float lo = (float)Math.Sqrt(Vector3.Dot(verlet.Velocity, verlet.Velocity));
                Vector3 vv = verlet.Velocity / lo;
                if (lo == 0) { vv = Vector3.Zero; }
                Vector3 Fric = -lo * lo * vv;



                
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
                

                verlet.Acc += Fric;

                // Helicopter lift (space to ascend, shift to descend)
                if (ctrlSpace)
                    verlet.Acc += Vector3.Up * 80f; // Ascend
                if (ctrlShift)
                    verlet.Acc -= Vector3.Up * 10f; // Descend

                // Forward/backward movement (W/S)
                if (ctrlW)
                    verlet.Acc += d * 80f; // Move forward
                if (ctrlS)
                    verlet.Acc -= d * 50f; // Move backward

                // Strafing (A/D)
                if (ctrlA)
                    verlet.Acc += -r * 50f; // Move left
                    
                if (ctrlD)
                   verlet.Acc += r * 50f; // Move right
                if (ctrlQ)
                {
                    verlet.Omega = new Vector3(0,1,0);
                }
                if (ctrlE)
                {
                    verlet.Omega = new Vector3(0, -1, 0);
                }

                    verlets[i]= verlet;
            }
            // Yaw (Q/E) - rotation around vertical axis (Up)
            // Forgás balra, ha "ctrlQ" lenyomva van
            /*
            if (ctrlQ)
            {
                verlets[0].Acc += r * 10f; // Gyorsítás balra forgáshoz
                verlets[3].Acc += r * 10f;
                verlets[1].Acc -= r * 10f; // Gyorsítás balra forgáshoz
                verlets[2].Acc -= r * 10f;
            }
            if (ctrlE)// Rotate right
            {
                verlets[0].Acc -= r * 10f; // Gyorsítás balra forgáshoz
                verlets[3].Acc -= r * 10f;
                verlets[1].Acc += r * 10f; // Gyorsítás balra forgáshoz
                verlets[2].Acc += r * 10f;
            }
            */
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
