using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.Event;
using Mugen.GFX;
using Mugen.ImGui;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using Mugen.Input;

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
        R,
        Count
    }

    public enum Layers
    {
        ImGui,
        Gui,
        Main,
        FrontFX,
        BackFX,
        Debug,
        Count
    }

    public class Game1 : Game
    {
        #region Attributes

        Gui.CheckBox _btnFullScreen;

        public const int ScreenW = 1920;
        public const int ScreenH = 1080;

        public static SpriteFont _fontMain;

        public static MouseCursor _mouseCursor;
        public static MouseCursor _mouseCursor2;
        public static MouseControl MouseControl;

        public static Texture2D _texMouseCursor;
        public static Texture2D _texMouseCursor2;

        public static Texture2D _texHeart;
        public static Texture2D _texFace;
        public static Texture2D _texAvatar1x1;
        public static Texture2D _texAvatar2x2;

        public static Texture2D _texBackground;
        public static Texture2D _texCursor;
        public static Texture2D _texTrail;

        public static Texture2D _texBtnBase;
        public static Texture2D _texBtnFullscreen;

        public static Effect _effectBasic;
        public static Effect _effectColor;

        public static SoundEffect _soundClock;

        public static string _hello = "Hello tout le monde";

        public Vector2 _mouse;
        public static MouseState _mouseState = new();

        private ScreenPlay _screenPlay;
        
        public WindowManager WM => _windowManager;
        private WindowManager _windowManager;
        private SpriteBatch _batch;
        private StateEvent _button;

        private static bool _isQuit = false;

        private ImGuiRenderer _imGuiRenderer;
        string inputText = "";
        private ImFontPtr guiFont;

        bool _isShowImGuiDebug = true;

        public static float _volumeMaster = .5f;
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
            base.Initialize();

            MouseControl = new();

            _screenPlay = new ScreenPlay(this);

            GFX.Init(GraphicsDevice); // don't forget to initialize when to draw GFX shapes etc
            _batch = new SpriteBatch(GraphicsDevice);

            ScreenManager.Init(_windowManager, _batch, _screenPlay.Init(), (int)Layers.Count);

            _button = new StateEvent((int)ButtonDown.Count);

            var style = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("Content/Misc/styleBtnFullscreen.json"));

            _btnFullScreen = (Gui.CheckBox)new Gui.CheckBox(MouseControl,"", style)
                .SetPosition(ScreenW - 20, 20);

        }
        protected override void LoadContent()
        {

            guiFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("Content\\Fonts\\homespun.ttf", 20);
            _imGuiRenderer.RebuildFontAtlas();

            _fontMain = Content.Load<SpriteFont>("Fonts/fontMain");
            _texHeart = Content.Load<Texture2D>("Images/Heart");
            _texFace = Content.Load<Texture2D>("Images/avatar00");
            _texBackground = Content.Load<Texture2D>("Images/background00");
            _texAvatar1x1 = Content.Load<Texture2D>("Images/avatar1x1");
            _texAvatar2x2 = Content.Load<Texture2D>("Images/avatar2x2");
            _texCursor = Content.Load<Texture2D>("Images/mouseCursor");
            _texTrail = Content.Load<Texture2D>("Images/trail");

            _texBtnBase = Content.Load<Texture2D>("Images/Button0");
            _texBtnFullscreen = Content.Load<Texture2D>("Images/ButtonFullscreen");

            _effectBasic = Content.Load<Effect>("Effects/effectBasic");
            _effectColor = Content.Load<Effect>("Effects/effectColor");

            _soundClock = Content.Load<SoundEffect>("Sounds/clock");


            _texMouseCursor = Content.Load<Texture2D>("Images/mouse_cursor");
            _texMouseCursor2 = Content.Load<Texture2D>("Images/mouse_cursor2");
            _mouseCursor = MouseCursor.FromTexture2D(_texMouseCursor, 0, 0);
            _mouseCursor2 = MouseCursor.FromTexture2D(_texMouseCursor2, 0, 0);
        }

        protected override void Update(GameTime gameTime)
        {
            FrameCounter.Update(gameTime);

            _mouseState = Mouse.GetState();
            _windowManager.Update(_mouseState.Position.ToVector2());
            _mouse = _windowManager.GetMousePosition();

            MouseControl.Update((int)_mouse.X, (int)_mouse.Y, Mouse.GetState().LeftButton == ButtonState.Pressed ? 1 : 0);

            if (MouseControl._isOverAny)
                Mouse.SetCursor(_mouseCursor2);
            else
                Mouse.SetCursor(_mouseCursor);

            _button.BeginSetEvents();
            _button.SetEvent((int)ButtonDown.F1, Keyboard.GetState().IsKeyDown(Keys.F1));
            _button.SetEvent((int)ButtonDown.F11, Keyboard.GetState().IsKeyDown(Keys.F11));
            _button.SetEvent((int)ButtonDown.R, Keyboard.GetState().IsKeyDown(Keys.R));

            if (_button.OnEvent((int)ButtonDown.F1))
            {
                _isShowImGuiDebug = !_isShowImGuiDebug;
            }

            if (_button.OnEvent((int)ButtonDown.R))
            {
                Console.WriteLine("Roll");
            }

            if (_button.OffEvent((int)ButtonDown.F11))
            {
                _windowManager.ToggleFullscreen();
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape) || _isQuit)
                Exit();

            ScreenManager.Update(gameTime);

            _btnFullScreen.Update(gameTime);

            if (_btnFullScreen._navi._onRelease)
            {
                _windowManager.ToggleFullscreen();
                _btnFullScreen.SetChecked(!_windowManager.IsFullscreen);
            }

            base.Update(gameTime);
        }
        public static void Quit()
        {
            _isQuit = true;
        }
        protected override void Draw(GameTime gameTime)
        {
            if (_isShowImGuiDebug)
            {
                ScreenManager.BeginDraw((int)Layers.ImGui);

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

                //Texture2D tex = Field.Get<Game1, Texture2D>("_texBtnBase");
                //_batch.Draw(tex, Vector2.One * 200, Color.White);

                ScreenManager.EndDraw();
            }


            ScreenManager.BeginDraw((int)Layers.Main, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.Main , gameTime);
            ScreenManager.EndDraw();


            ScreenManager.BeginDraw((int)Layers.BackFX, SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.BackFX, gameTime);
            ScreenManager.EndDraw();
            
            ScreenManager.BeginDraw((int)Layers.FrontFX, SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.FrontFX, gameTime);
            ScreenManager.EndDraw();


            ScreenManager.BeginDraw((int)Layers.Debug, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.Debug, gameTime);
            ScreenManager.EndDraw();

            ScreenManager.BeginDraw((int)Layers.Gui, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.DrawLayer((int)Layers.Gui, gameTime);
            _btnFullScreen.Draw(_batch, gameTime,(int)Layers.Gui);
            ScreenManager.EndDraw();


            ScreenManager.BeginShow(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap);
            ScreenManager.ShowLayer((int)Layers.BackFX, Color.White);
            ScreenManager.ShowLayer((int)Layers.Main, Color.White);
            ScreenManager.ShowLayer((int)Layers.Gui, Color.White);
            ScreenManager.ShowLayer((int)Layers.FrontFX, Color.White);


            if (_isShowImGuiDebug)
            {
                ScreenManager.ShowLayer((int)Layers.Debug, Color.White);
                ScreenManager.ShowLayer((int)Layers.ImGui, Color.White);
            }

            FrameCounter.Draw(_batch, _fontMain, Color.Yellow, 10, 10);

            ScreenManager.EndShow();


            base.Draw(gameTime);
        }
    }
}
