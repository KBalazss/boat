using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoatSim
{
    internal class HUD
    {
        SpriteFont font;
        int framesInCurrentSecond, currentSecond, fps;
        public HUD(SpriteFont font)
        {
            this.font = font;
        }
        public void Draw(SpriteBatch spriteBatch, List<Boat> boats, Camera cam)
        {
            var now = DateTime.Now;
            if (now.Second != currentSecond)
            {
                fps = framesInCurrentSecond;
                framesInCurrentSecond = 0;
                currentSecond = now.Second;
            }
            framesInCurrentSecond++;
            spriteBatch.DrawString(font, "FPS: " + fps, new Vector2(10, 10), Color.White);


            foreach (var b in boats)
            {
                var screenSpace = Vector4.Transform(b.Position, cam.View * cam.Projection);
                var z = screenSpace.Z / screenSpace.W;
                if (z < 0 || z > 1)
                    continue;
                var screenPos = new Vector2(
                    (1 + screenSpace.X / screenSpace.W) * spriteBatch.GraphicsDevice.Viewport.Width / 2,
                    (1 - screenSpace.Y / screenSpace.W) * spriteBatch.GraphicsDevice.Viewport.Height / 2);
                var nameSize = font.MeasureString(b.Name);
                spriteBatch.DrawString(font, b.Name,
                    screenPos + new Vector2(-nameSize.X / 2, z * 110 - 150 - nameSize.Y), Color.White);
            }

            spriteBatch.DrawString(font, "Speed [m/s]: " + boats[0].verlets[0].Velocity.Length().ToString("f00"),
                new Vector2(10, 30), Color.White);
            spriteBatch.DrawString(font, "Boats: " + boats.Count.ToString(),
                new Vector2(10, 50), Color.White);

        }
    }
}
