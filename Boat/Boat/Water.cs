using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class Water
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        Effect effect;
        Texture2D normalMap;
        public Water(GraphicsDevice dev, Texture2D normalMap, Effect effect)
        {
            this.normalMap = normalMap;
            vertexBuffer = new VertexBuffer(dev, VertexPosition.VertexDeclaration, 4, BufferUsage.WriteOnly);
            vertexBuffer.SetData(new VertexPosition[] {
                new VertexPosition( new Vector3( 1, 0, 1 ) ),
                new VertexPosition( new Vector3( -1, 0, 1 ) ),
                new VertexPosition( new Vector3( 1, 0, -1 ) ),
                new VertexPosition( new Vector3( -1, 0, -1 ) )
            });
            indexBuffer = new IndexBuffer(dev, typeof(ushort), 4, BufferUsage.WriteOnly);
            indexBuffer.SetData(new ushort[] { 0, 1, 2, 3 });
            this.effect = effect;
        }
        public void Draw(Camera cam, RenderTarget2D waterReflection, RenderTarget2D waterRefraction,
            RenderTarget2D heightMap, GameTime gameTime, Vector3 sunDir)
        {
            var dev = vertexBuffer.GraphicsDevice;
            dev.SetVertexBuffer(vertexBuffer);
            dev.Indices = indexBuffer;
            var world = Matrix.CreateScale(1000) *
                Matrix.CreateTranslation(new Vector3(cam.Position.X, 0, cam.Position.Z));
            effect.Parameters["CamPos"].SetValue(cam.Position);
            effect.Parameters["SunDir"].SetValue(sunDir);
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["WorldViewProj"].SetValue(world * cam.View * cam.Projection);
            effect.Parameters["ReflectionMap"].SetValue(waterReflection);
            effect.Parameters["RefractionMap"].SetValue(waterRefraction);
            effect.Parameters["HeightMap"].SetValue(heightMap);
            effect.Parameters["NormalMap"].SetValue(normalMap);
            effect.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            effect.CurrentTechnique.Passes[0].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 2);
        }
    }
}
