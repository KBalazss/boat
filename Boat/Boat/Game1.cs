using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BoatSim
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        BasicGeometry cube;
        Sky sky;
        Boat boat;
        Helicopter helicopter;
        Island island;
        Water water;
        RenderTarget2D waterReflection;
        RenderTarget2D waterRefraction;
        RenderTarget2D heightMap;
        Network network = new ();
        List<Boat> boats = new ();
        TimeSpan lastSendTime;
        Stopwatch uptime = Stopwatch.StartNew();
        HUD hud;
        Boolean vehicle;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            //_graphics.SynchronizeWithVerticalRetrace = false;
            //IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            base.Initialize();
            waterReflection = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
            waterRefraction = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.Depth16);
            heightMap = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, false, SurfaceFormat.Single, DepthFormat.Depth16);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            cube = BasicGeometry.CreateRoundedCube(GraphicsDevice, .2f);
            sky = new Sky(GraphicsDevice, Content.Load<Texture2D>("skyhalf"));
            boat = new Boat(GraphicsDevice, Content.Load<Model>("boat"));
            helicopter = new Helicopter(GraphicsDevice, Content.Load<Model>("Helecopter"));
            island = new Island(GraphicsDevice, Content.Load<Texture2D>("islandColor"),
                Content.Load<Texture2D>("islandHeight"), sky.SunDir, Content.Load<Effect>("Island"));
            water = new Water(GraphicsDevice, Content.Load<Texture2D>("wave2"),
                Content.Load<Effect>("Water"));
            boats.Add(boat);
            hud = new HUD(Content.Load<SpriteFont>("Segoe"));
        }

        void SendAndReceive()
        {
            var now = uptime.Elapsed;
            if (now - lastSendTime > TimeSpan.FromMilliseconds(100))
            {
                lastSendTime = now;
                network.Send(boat.PackState());
            }

            var dtos = network.Receive();
            if (dtos != null)
            {
                for (int i = 0; i < dtos.Count; i++)
                {
                    var b = boats.Find(x => x.Id == dtos[i].Id);
                    if (b == null)
                        boats.Add(b = new Boat(boat, dtos[i]) { Id = dtos[i].Id });
                    b.UnpackState(dtos[i]);
                }
                boats.RemoveAll(x => x != boat && dtos.TrueForAll( y => y.Id != x.Id));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.V))
            {
                if (vehicle)
                {
                    // Switch from boat to helicopter
                    helicopter.SetPosition(boat.Position); // Match helicopter's position to the boat
                }
                else
                {
                    // Switch from helicopter to boat
                    boat.SetPosition(helicopter.Position); // Match boat's position to the helicopter
                }

                vehicle = !vehicle; // Toggle between boat and helicopter
            }


            var keyboard = Keyboard.GetState();
            if (vehicle)
            {
                boat.ctrlW = keyboard.IsKeyDown(Keys.W);
                boat.ctrlA = keyboard.IsKeyDown(Keys.A);
                boat.ctrlS = keyboard.IsKeyDown(Keys.S);
                boat.ctrlD = keyboard.IsKeyDown(Keys.D);
                foreach (Boat b in boats)
                {
                    b.Step();
                }
            }
            else
            {
                helicopter.ctrlD = keyboard.IsKeyDown(Keys.D);
                helicopter.ctrlA = keyboard.IsKeyDown(Keys.A);
                helicopter.ctrlS = keyboard.IsKeyDown(Keys.S);
                helicopter.ctrlW = keyboard.IsKeyDown(Keys.W);
                helicopter.ctrlSpace = keyboard.IsKeyDown(Keys.Space);
                helicopter.ctrlShift = keyboard.IsKeyDown(Keys.LeftShift);
                helicopter.ctrlQ = keyboard.IsKeyDown(Keys.Q);
                helicopter.ctrlE = keyboard.IsKeyDown(Keys.E);
                helicopter.ctrlB = keyboard.IsKeyDown(Keys.B);
                helicopter.Step();
            }
            SendAndReceive();
            ResolveCollisions();

            base.Update(gameTime);
        }

        void ResolveCollisions()
        {
            for (int i = 0; i < boats.Count - 1; i++)
                for (int j = i + 1; j < boats.Count; j++)
                    Boat.Collide(boats[i], boats[j]);
        }

        float rot;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            if (vehicle)
            {
                Camera.Main.Position = boat.Position - Vector3.Normalize(boat.Direction) * 9 + Vector3.Up * 5;
                Camera.Main.Direction = Vector3.Normalize(boat.Position - Camera.Main.Position);
                Camera.Main.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            }
            else
            {
                Camera.Main.Position = helicopter.Position - Vector3.Normalize(helicopter.Direction) * 9 + Vector3.Up * 5;
                Camera.Main.Direction = Vector3.Normalize(helicopter.Position - Camera.Main.Position);
                Camera.Main.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            }

            var islandWaterLevel = Matrix.CreateTranslation(0, -0.05f, 0);
            var islandTransforms = new Matrix[]
            {
                islandWaterLevel * Matrix.CreateScale(50,10,50 )* Matrix.CreateTranslation(100,0,0),
                islandWaterLevel * Matrix.CreateScale(100,20,100 ) * Matrix.CreateTranslation(-50,0,70 ),
                islandWaterLevel * Matrix.CreateScale(70,15,70) * Matrix.CreateTranslation(-50,0,-70 )
            };

            // Reflection map
            GraphicsDevice.SetRenderTarget(waterReflection);
            GraphicsDevice.Clear(Color.Black);
            var reflectionCam = Camera.Main.GetReflection(Vector3.Up);
            sky.Draw(reflectionCam);
            if (vehicle) {
                foreach (Boat b in boats)
                    b.Draw(reflectionCam, sky.SunDir);
            } else { helicopter.Draw(reflectionCam, sky.SunDir); }

            foreach (var m in islandTransforms)
                island.Draw(m, reflectionCam, gameTime);


            // height map
            GraphicsDevice.SetRenderTarget(heightMap);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer,
                new Vector4(-100.0f, 0, 0, 0), 1, 0);
            foreach (var m in islandTransforms)
                island.DrawHeight(m, Camera.Main, gameTime);

            // Refraction
            GraphicsDevice.SetRenderTarget(waterRefraction);
            GraphicsDevice.Clear(Color.Black);
            foreach (var m in islandTransforms)
                island.DrawRefraction(m, Camera.Main, gameTime);

            // Normal render with Camera.Main
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Pink);
            sky.Draw(Camera.Main);
            if (vehicle)
            {
                foreach (Boat b in boats)
                    b.Draw(Camera.Main, sky.SunDir);
            } else {
                helicopter.Draw(Camera.Main, sky.SunDir);
            }

            foreach (var m in islandTransforms)
                island.Draw(m, Camera.Main, gameTime);
            water.Draw(Camera.Main, waterReflection, waterRefraction, heightMap, gameTime, sky.SunDir);


            _spriteBatch.Begin();
            //_spriteBatch.Draw(waterReflection, new Rectangle(100, 100, 500, 200), Color.White);
            //hud.Draw(_spriteBatch, boats, Camera.Main);
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
