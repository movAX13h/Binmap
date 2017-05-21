using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Forms = System.Windows.Forms;
using System;
using System.IO;
using Binmap.Screens;
using System.Linq;
using Binmap.Controls;

namespace Binmap
{
    public class Main : Game
    {
        public static MouseState MouseState { get; private set; }
        public static KeyboardState KeyboardState { get; private set; }

        public static Texture2D WhiteTexture { get; private set; }
        public static SpriteFont DefaultFont;

        public static SpriteFont FontS;
        public static SpriteFont FontL;

        public static Texture2D Logo;
        public static Effect IntroShader;

        public static Color BackgroundColor = Color.FromNonPremultiplied(45, 45, 48, 255);
        public static Color PanelColor = Color.FromNonPremultiplied(30, 30, 30, 255);
        public static Color BorderColor = Color.FromNonPremultiplied(62, 62, 66, 255);
        public static Color TrackColor = Color.FromNonPremultiplied(104, 104, 104, 255);

        public static Color HexColor = Color.FromNonPremultiplied(141, 210, 138, 255);
        public static Color BinColor = Color.FromNonPremultiplied(215, 172, 106, 255);
        public static Color DecColor = Color.FromNonPremultiplied(0, 122, 204, 255);
        public static Color NibDecColor = Color.FromNonPremultiplied(210, 148, 226, 255);
        public static Color AsciiColor = Color.Yellow;

        private Keys[] keysPressed;
        private Keys[] keysReleased;
        private static IInput focusedControl;

        public static void SetFocus(IInput control)
        {
            if (focusedControl != null) focusedControl.Focused = false;
            focusedControl = control;
            if (focusedControl != null) focusedControl.Focused = true;
        }

        public static string Version = "1.5";

        Keys[] prevPressed;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Layouter layouter;
        float time;
        string initialFile;

        public Main(string file)
        {
            initialFile = file;

            Window.Title = "Binmap " + Version;

            Forms.Form frm = (Forms.Form)Forms.Form.FromHandle(Window.Handle);
            //frm.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            frm.AllowDrop = true;
            frm.DragEnter += new Forms.DragEventHandler(windowDragEnter);
            frm.DragDrop += new Forms.DragEventHandler(windowDragDrop);
            
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsMouseVisible = true;

            //Window.IsBorderless = true;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

        }

        protected override void Initialize()
        {
            base.Initialize();
            Window.Position = new Point((GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - GraphicsDevice.Viewport.Width) / 2,
                                        (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - GraphicsDevice.Viewport.Height) / 2);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            WhiteTexture = new Texture2D(GraphicsDevice, 10, 10, false, SurfaceFormat.Color);
            
            FontS = Content.Load<SpriteFont>("Fonts/Font1");
            FontS.LineSpacing = 12;

            FontL = Content.Load<SpriteFont>("Fonts/Font2");
            FontL.LineSpacing = 14;

            Logo = Content.Load<Texture2D>("Logo");

            IntroShader = Content.Load<Effect>("Shaders/Intro");

            DefaultFont = FontL;

            Color[] colorData = new Color[100];
            for (int i = 0; i < 100; i++) colorData[i] = Color.White;
            WhiteTexture.SetData<Color>(colorData);
            
            layouter = new Layouter(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            if (initialFile != "") loadFile(initialFile);
        }

        protected override void UnloadContent()
        {
        }

        private void windowDragDrop(object sender, Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(Forms.DataFormats.FileDrop);
            loadFile(files[0]);
        }

        private void windowDragEnter(object sender, Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Forms.DataFormats.FileDrop)) e.Effect = Forms.DragDropEffects.Copy;
        }

        private void loadFile(string filename)
        {
            if (!File.Exists(filename)) return;
            
            if (layouter.LoadFile(filename))
            {
                string a = Path.GetFileName(layouter.DataFilename);
                string b = "";
                if (layouter.BinmapFilename != string.Empty)
                {
                    b = Path.GetFileNameWithoutExtension(layouter.BinmapFilename);
                    if (a == b) b = "";
                    else b = " (" + b + ")";
                }

                Window.Title = "Binmap " + Version + " - " + a + b + ", " + layouter.NumBytes + " bytes";
            }
            else Forms.MessageBox.Show(layouter.LastError, "Sorry!", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
        }

        protected override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                return;
            }

            #region update input statics
            MouseState = Mouse.GetState();
            KeyboardState = Keyboard.GetState();
            
            if (prevPressed != null)
            {
                keysReleased = prevPressed.Except(KeyboardState.GetPressedKeys()).ToArray(); // released since the last frame
                keysPressed = KeyboardState.GetPressedKeys().Except(prevPressed).ToArray(); // pressed in this frame

                if (keysPressed.Length > 0)
                {
                    if (focusedControl != null)
                    {
                        focusedControl.ProcessKey(keysPressed[0]);
                    }
                }
            }
            else
            {
                keysReleased = new Keys[0];
                keysPressed = new Keys[0];
            }
            prevPressed = KeyboardState.GetPressedKeys();
            #endregion

            float dTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            time += dTime;

            layouter.Update(time, dTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.FromNonPremultiplied(45, 45, 48, 255));
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            layouter.Draw(spriteBatch);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width < 10 || Window.ClientBounds.Height < 10) return;

            if (graphics.PreferredBackBufferWidth != Window.ClientBounds.Width ||
                graphics.PreferredBackBufferHeight != Window.ClientBounds.Height)
            {
                graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;

                graphics.ApplyChanges();
                layouter.Resize(Math.Max(404, GraphicsDevice.Viewport.Width), Math.Max(256, GraphicsDevice.Viewport.Height));
            }
        }
    }
}
