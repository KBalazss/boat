using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BoatSim
{
    internal class Island
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;
        Effect effect;
        public Island(GraphicsDevice dev, Texture2D tex, Texture2D heightMapTex, Vector3 sunDir, Effect effect )
        {
            int w = heightMapTex.Width;
            int h = heightMapTex.Height;
            uint[] hm = new uint[w * h];
            var vbData = new VertexPositionNormalTexture[hm.Length];
            heightMapTex.GetData(hm);
            for (int i = 0; i < hm.Length; i++)
            {
                float height = (hm[i] & 255) / 255.0f;
                int x = i % w;
                int y = i / w;
                var pos = new Vector3(x / (float)(w - 1), height, y / (float)(h - 1));
                vbData[i] = new VertexPositionNormalTexture(pos, Vector3.Up, new Vector2(pos.X, pos.Z));
            }

            for (int i = 0; i < hm.Length; i++)
            {
                int x = i % w;
                int y = i / w;
                if (x != 0 && x != w - 1 && y != 0 && y != h - 1)
                {
                    var xdir = vbData[y * w + x + 1].Position - vbData[y * w + x - 1].Position;
                    var zdir = vbData[(y + 1) * w + x].Position - vbData[(y - 1) * w + x].Position;
                    vbData[i].Normal = Vector3.Normalize(Vector3.Cross(zdir, xdir));
                }
            }

            vertexBuffer = new VertexBuffer(dev, VertexPositionNormalTexture.VertexDeclaration, vbData.Length,
            BufferUsage.WriteOnly);
            vertexBuffer.SetData(vbData);
            var indices = new ushort[(h - 1) * (w * 2 + 1)];
            int idx = 0;
            int dir = 1;
            for (int j = 1; j < w; j++)
            {
                int i = dir > 0 ? 0 : w - 1;
                for (; i >= 0 && i < w; i += dir)
                {
                    indices[idx++] = (ushort)(j * w + i);
                    indices[idx++] = (ushort)((j - 1) * w + i);
                }
                indices[idx++] = (ushort)((j) * w + i - dir);
                dir = -dir;
            }
            indexBuffer = new IndexBuffer(dev, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);

            this.effect = effect;
            effect.Parameters["DiffuseMap"].SetValue(tex);
            effect.Parameters["SunDir"].SetValue(sunDir);
        }

        public void Draw(Matrix world, Camera cam, GameTime gameTime)
        {
            var dev = vertexBuffer.GraphicsDevice;
            dev.SetVertexBuffer(vertexBuffer);
            dev.Indices = indexBuffer;
            effect.Parameters["WorldViewProj"].SetValue(world * cam.View * cam.Projection);
            effect.Parameters["WorldIT"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["toneMappingStrength"].SetValue(gameTime.TotalGameTime.Milliseconds/1000.0f);
            effect.CurrentTechnique.Passes[0].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, indexBuffer.IndexCount - 2);
        }

        public void DrawHeight(Matrix world, Camera cam, GameTime gameTime)
        {
            var dev = vertexBuffer.GraphicsDevice;
            dev.SetVertexBuffer(vertexBuffer);
            dev.Indices = indexBuffer;
            effect.Parameters["WorldViewProj"].SetValue(world * cam.View * cam.Projection);
            effect.Parameters["WorldIT"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["toneMappingStrength"].SetValue(gameTime.TotalGameTime.Milliseconds / 1000.0f);
            effect.CurrentTechnique.Passes[1].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, indexBuffer.IndexCount - 2);
        }

        public void DrawRefraction(Matrix world, Camera cam, GameTime gameTime)
        {
            var dev = vertexBuffer.GraphicsDevice;
            dev.SetVertexBuffer(vertexBuffer);
            dev.Indices = indexBuffer;
            effect.Parameters["WorldViewProj"].SetValue(world * cam.View * cam.Projection);
            effect.Parameters["WorldIT"].SetValue(Matrix.Transpose(Matrix.Invert(world)));
            effect.Parameters["World"].SetValue(world);
            effect.Parameters["toneMappingStrength"].SetValue(gameTime.TotalGameTime.Milliseconds / 1000.0f);
            effect.CurrentTechnique.Passes[2].Apply();
            dev.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, indexBuffer.IndexCount - 2);
        }


    }
}
