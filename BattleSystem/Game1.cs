using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.Event;
using Mugen.GFX;
using Mugen.ImGui;
using System;

namespace BattleSystem
{
    enum ButtonDown
    {
        A,
        B,
        X,
        Y,
        F1,
        F11,
        Space,
        Count
    }

    public enum Layers
    {
        Gui,
        Main,
        FX,
        Debug,
        Count
    }

    public class Game1 : Game
    {
        #region Attributes
        public const int ScreenW = 1920;
        public const int ScreenH = 1080;

        public static SpriteFont _fontMain;
        
        public static Texture2D _texHeart;
        public static Texture2D _texFace;
        public static Texture2D _texAvatar1x1;
        public static Texture2D _texBackground;
        public static Texture2D _texGlow0;
        public static Texture2D _texCursor;
        public static Texture2D _texTrail;

        public static Effect _effectBasic;
        public static Effect _effectColor;

        public Vector2 _mouse;

        private ScreenPlay _screenPlay;

        public WindowManager WM => _windowManager;
        private WindowManager _windowManager;
        private SpriteBatch _batch;
        private StateEvent _button;

        //RenderTarget2D _targetAlphaBlend;
        //RenderTarget2D _targetAdditive;
        //RenderTarget2D _imGuiRenderTarget;

        private ImGuiRenderer _imGuiRenderer;
        string inputText = "";
        private ImFontPtr guiFont;

        RasterizerState _rasterizerState;
        bool _isShowGui = false;
        #endregion

        public Game1()
        {
            _windowManager = new WindowManager(this, ScreenW, ScreenH);
            _windowManager.SetScale(1.5f);
            
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.Title = $"-- Native Resolution = {ScreenW}x{ScreenH}";

            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
        }
        public static bool IsLayer(int indexLayer)
        {
            return indexLayer == (int)Layers.Count;
        }
        protected override void Initialize()
        {
            _screenPlay = new ScreenPlay(this);

            GFX.Init(GraphicsDevice); // don't forget to initialize when to draw GFX shapes etc
            _batch = new SpriteBatch(GraphicsDevice);

            ScreenManager.Init(_windowManager, _batch, _screenPlay.Init(), (int)Layers.Count);

            _button = new StateEvent((int)ButtonDown.Count);

            base.Initialize();

            //Mouse.SetCursor(MouseCursor.FromTexture2D(_texCursor, 10, 2));
            
        }
        protected override void LoadContent()
        {

            guiFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("Content\\Fonts\\homespun.ttf", 20);
            _imGuiRenderer.RebuildFontAtlas();

            _fontMain = Content.Load<SpriteFont>("Fonts/fontMain");
            _texHeart = Content.Load<Texture2D>("Images/Heart");
            _texFace = Content.Load<Texture2D>("Images/avatar00");
            _texBackground = Content.Load<Texture2D>("Images/background00");
            _texGlow0 = Content.Load<Texture2D>("Images/circleGlow0");
            _texAvatar1x1 = Content.Load<Texture2D>("Images/avatar1x1");
            _texCursor = Content.Load<Texture2D>("Images/mouseCursor");
            _texTrail = Content.Load<Texture2D>("Images/trail");


            _effectBasic = Content.Load<Effect>("Effects/effectBasic");
            _effectColor = Content.Load<Effect>("Effects/effectColor");

        }

        protected override void Update(GameTime gameTime)
        {
            FrameCounter.Update(gameTime);

            _windowManager.Update(Mouse.GetState().Position.ToVector2());
            _mouse = _windowManager.GetMousePosition();

            _button.BeginSetEvents();
            _button.SetEvent((int)ButtonDown.F1, Keyboard.GetState().IsKeyDown(Keys.F1));
            _button.SetEvent((int)ButtonDown.F11, Keyboard.GetState().IsKeyDown(Keys.F11));
            _button.SetEvent((int)ButtonDown.Space, Keyboard.GetState().IsKeyDown(Keys.Space));

            if (_button.OnEvent((int)ButtonDown.F1))
            {
                _isShowGui = !_isShowGui;
            }

            if (_button.OnEvent((int)ButtonDown.Space))
            {
                Console.WriteLine("Roll");
            }

            if (_button.OffEvent((int)ButtonDown.F11))
            {
                //Console.WriteLine("Toggle Fullscreen");
                _windowManager.ToggleFullscreen();
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            ScreenManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (_isShowGui)
            {
                ScreenManager.BeginDraw((int)Layers.Gui);

                GraphicsDevice.Clear(Color.Transparent);
                _imGuiRenderer.BeforeLayout(_mouse.X, _mouse.Y, gameTime, Window.ClientBounds.Width, Window.ClientBounds.Height);
                ImGui.PushFont(guiFont);
                ImGui.SetNextWindowBgAlpha(1f);
                ImGui.Text($"Mouse = {_mouse.X}-{_mouse.Y}");
                ImGui.Text($"Monitor = {GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width}x{GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height}");
                ImGui.Text($"Window = {Window.ClientBounds.Width}x{Window.ClientBounds.Height}");
                ImGui.InputText("Text", ref inputText, 32);
                //ImGui.ShowDemoWindow();
                _imGuiRenderer.AfterLayout();
                //GFX.Draw(_batch, _texHeart, Color.White * .5f, 0, _mouse, Position.CENTER, Vector2.One * .5f);
                //GFX.Sight(_batch, _mouse.X, _mouse.Y, ScreenW, ScreenH, Color.OrangeRed, 1f);
                _batch.Draw(GFX._mouseCursor, _mouse, Color.Yellow);

                ScreenManager.EndDraw();
            }


            ScreenManager.BeginDraw((int)Layers.Main, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.Main , gameTime);
            ScreenManager.EndDraw();


            ScreenManager.BeginDraw((int)Layers.FX, SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.FX, gameTime);
            ScreenManager.EndDraw();


            ScreenManager.BeginDraw((int)Layers.Debug, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.Debug, gameTime);
            ScreenManager.EndDraw();


            ScreenManager.BeginShow(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.ShowLayer((int)Layers.FX, Color.White);
            ScreenManager.ShowLayer((int)Layers.Main, Color.White);
            ScreenManager.ShowLayer((int)Layers.Debug, Color.White);

            if (_isShowGui)
                ScreenManager.ShowLayer((int)Layers.Gui, Color.White);

            FrameCounter.Draw(_batch, _fontMain, Color.Yellow, 10, 10);

            ScreenManager.EndShow();


            base.Draw(gameTime);
        }
    }
}
