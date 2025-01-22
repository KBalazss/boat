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
        public Vector3 alfa;
        public Vector3 alfaMax = new Vector3((float)Math.PI/4,0, (float)Math.PI / 6); 
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
            };
            verletOrig = new Verlet[]
            {
                new Verlet( pos + new Vector3( size, 0, -size ) ), // Front-right
                new Verlet( pos + new Vector3( size, 0, size ) ),  // Back-right
                new Verlet( pos + new Vector3( -size, 0, size ) ), // Back-left
                new Verlet( pos + new Vector3( -size, 0, -size ) ),// Front-left
                new Verlet( pos + new Vector3( 0, 0, 0 ) ),        // Center (rotor)
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
            //ApplyConstraintsheli();
            collision.WorldTransform = WorldTransform;
        }

        private void ApplyForces()
        {

             //Gravity Force
            Vector3 gravity = new Vector3(0, -9.81f, 0);

            Vector3 d = Vector3.Normalize(Direction);
            Vector3 r = Vector3.Normalize(Right);
            Vector3 u = Vector3.Normalize(Up);

            for (int i = 0; i < verlets.Length; i++)
            {

                var verlet = verlets[i];
                //verlet.Acc = Vector3.Zero;
                verlet.Omega = Vector3.Zero;
                verlet.Acc = gravity;
                float lo = (float)Math.Sqrt(Vector3.Dot(verlet.Velocity, verlet.Velocity));
                Vector3 vv = verlet.Velocity / lo;
                if (lo == 0) { vv = Vector3.Zero; }
                Vector3 Fric = -lo * lo * vv;

                verlet.Acc += Fric;
                
                if (ctrlSpace)
                    verlet.Acc += u * 180f; // Ascend

                if (ctrlQ)
                    verlet.Omega = new Vector3(0, 1, 0);

                if (ctrlE)
                    verlet.Omega = new Vector3(0, -1, 0);

                verlets[i]= verlet;
            }

            if (ctrlW)
            {
                if (d.Y >= -Math.Sin(alfaMax.X))
                {
                    //tilt(new Vector3(-0.01f, 0, 0));
                    tilt(-r * 0.01f);
                }
            }
            else if (ctrlS)
            {
                if (d.Y <= Math.Sin(alfaMax.X))
                {
                    //tilt(new Vector3(0.01f, 0, 0));
                    tilt(r * 0.01f);
                }
            }
            else
            {
                if (Math.Abs(d.Y) <= Math.Sin(0.01f))
                {
                    float a = (float)Math.Asin(d.Y);
                    if (a != 0)
                    {
                           tilt(-r * a);
                    }
                    
                }

                if ( d.Y > Math.Asin(0.01f))
                {
                    tilt(-r * 0.01f);
                }
                else if (d.Y < -Math.Asin(0.01f)) { tilt(r * 0.01f); }
       
            }
            if (ctrlA)
            {
                if (r.Y <= Math.Sin(alfaMax.Z))
                {
                    //tilt(new Vector3(0, 0, 0.01f));
                    tilt(-d * 0.01f);
                }
            }
            else if (ctrlD)
            {
                if (r.Y >= -Math.Sin(alfaMax.Z))
                {
                    //tilt(new Vector3(0, 0, -0.01f));
                    tilt(d * 0.01f);
                }
            }
            else
            {
                //if (Math.Abs(alfa.Z) <= 0.01f )
                    alfa.Z = 0;

                //tilt(new Vector3(0, 0,0.01f * -Math.Sign(alfa.Z)));
                if (r.Y > Math.Asin(0.01f))
                {
                    tilt(d * 0.01f);
                }
                else if (r.Y < -Math.Asin(0.01f)) { tilt(-d * 0.01f); }

            }
        }
        
        public void tiltf(float tiltAmount)
        {
            float frontTilt = -tiltAmount; // Lower the front
            float backTilt = tiltAmount;  // Raise the back


            // Adjust Y positions of front and back Verlets
            verlets[0].Pos.Y += frontTilt; // Front-right
            verlets[3].Pos.Y += frontTilt; // Front-left
            verlets[1].Pos.Y += backTilt;  // Back-right
            verlets[2].Pos.Y += backTilt;  // Back-left
        }
        public void tiltside(float tiltAmount)
        {
            float frontTilt = -tiltAmount; // Lower the front
            float backTilt = tiltAmount;  // Raise the back
            
            // Adjust Y positions of front and back Verlets
            verlets[0].Pos.Y += frontTilt; // Front-right
            verlets[1].Pos.Y += frontTilt;  // Back-right
            verlets[3].Pos.Y += backTilt; // Front-left
            verlets[2].Pos.Y += backTilt;  // Back-left
        }

        public void tilt(Vector3 dAlfa)
        {
            verlets[0].Pos += Vector3.Cross(dAlfa, verlets[0].Pos - verlets[4].Pos);
            verlets[1].Pos += Vector3.Cross(dAlfa, verlets[1].Pos - verlets[4].Pos);
            verlets[2].Pos += Vector3.Cross(dAlfa, verlets[2].Pos - verlets[4].Pos);
            verlets[3].Pos += Vector3.Cross(dAlfa, verlets[3].Pos - verlets[4].Pos);
            alfa += dAlfa;

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
        private void ApplyConstraintsheli()
        {
            Verlet.ApplyLengthConstraint(ref verlets[0], ref verlets[1], 2f); // Front-right to Back-right
            Verlet.ApplyLengthConstraint(ref verlets[1], ref verlets[2], 2f); // Back-right to Back-left
            Verlet.ApplyLengthConstraint(ref verlets[2], ref verlets[3], 2f); // Back-left to Front-left
            Verlet.ApplyLengthConstraint(ref verlets[3], ref verlets[0], 2f); // Front-left to Front-right
            Verlet.ApplyLengthConstraint(ref verlets[0], ref verlets[4], 1.5f); // Front-right to Center
            Verlet.ApplyLengthConstraint(ref verlets[1], ref verlets[4], 1.5f); // Back-right to Center
            Verlet.ApplyLengthConstraint(ref verlets[2], ref verlets[4], 1.5f); // Back-left to Center
            Verlet.ApplyLengthConstraint(ref verlets[3], ref verlets[4], 1.5f); // Front-left to Center
        }
    }
}
