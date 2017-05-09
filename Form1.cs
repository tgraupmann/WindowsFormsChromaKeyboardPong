using Corale.Colore.Core;
using Corale.Colore.Razer.Keyboard;
using Gma.System.MouseKeyHook;
using System;
using System.Windows.Forms;

namespace WindowsFormsChromaKeyboardPong
{
    public partial class Form1 : Form
    {
        private static IKeyboardEvents _mKeyboardEvents = null;

        private static readonly Color COLOR_SCORE_P1 = new Color(0f, 1f, 1f);
        private static readonly Color COLOR_SCORE_P2 = Color.Blue;
        private static readonly Color COLOR_BLOCK = new Color(1f, 0.5f, 0f);
        private static readonly float BALL_SPEED = 0.5f;

        private class Vector2
        {
            public static readonly Vector2 Right = new Vector2(1, 0);
            public static readonly Vector2 Zero = new Vector2();
            public float _mX = 0f;
            public float _mY = 0f;
            public Vector2()
            {
                _mX = 0;
                _mY = 0;
            }
            public Vector2(float x, float y)
            {
                _mX = x;
                _mY = y;
            }
            public static Vector2 operator +(Vector2 a, Vector2 b)
            {
                return new Vector2(a._mX + b._mX, a._mY + b._mY);
            }
            public static Vector2 operator -(Vector2 a, Vector2 b)
            {
                return new Vector2(a._mX - b._mX, a._mY - b._mY);
            }
            public static Vector2 operator -(Vector2 a)
            {
                return new Vector2(-a._mX, -a._mY);
            }
            public static Vector2 operator *(Vector2 a, float b)
            {
                return new Vector2(a._mX * b, a._mY * b);
            }
            public static Vector2 operator *(float a, Vector2 b)
            {
                return new Vector2(a * b._mX, a * b._mY);
            }
        }

        private class BallData
        {
            public Vector2 _mPosition = Vector2.Zero;
            public Vector2 _mDirection = Vector2.Right;
            public int GetColumn()
            {
                return (int)_mPosition._mX;
            }
            public int GetRow()
            {
                return (int)_mPosition._mY;
            }
        }

        private class KeyData
        {
            public Key _mKey;
            public Color _mColor;
            public static implicit operator KeyData(Key key)
            {
                KeyData keyData = new KeyData();
                keyData._mKey = key;
                keyData._mColor = Color.Black;
                return keyData;
            }
        }

        #region Key layout

        private static KeyData[,] _sKeys =
        {
            {
                Key.Q,
                Key.W,
                Key.E,
                Key.R,
                Key.T,
                Key.Y,
                Key.U,
                Key.I,
                Key.O,
                Key.P,
                Key.Oem4, //[
                Key.Oem5, //]
            },
            {
                Key.A,
                Key.S,
                Key.D,
                Key.F,
                Key.G,
                Key.H,
                Key.J,
                Key.K,
                Key.L,
                Key.Oem7, //;
                Key.Oem8, //'
                Key.Invalid,
            },
            {
                Key.Z,
                Key.X,
                Key.C,
                Key.V,
                Key.B,
                Key.N,
                Key.M,
                Key.Oem9, //,
                Key.Oem10, //.
                Key.Oem11, //?
                Key.Invalid,
                Key.Invalid,
            },
        };

        #endregion

        private static KeyData[,] _sPlayer1Keys =
        {
            {
                Key.Q,
                Key.W,
                Key.E,
                Key.R,
                Key.T,
            },
            {
                Key.A,
                Key.S,
                Key.D,
                Key.F,
                Key.G,
            },
            {
                Key.Z,
                Key.X,
                Key.C,
                Key.V,
                Key.Invalid,
            },
        };

        private static KeyData[,] _sPlayer2Keys =
        {
            {
                Key.Y,
                Key.U,
                Key.I,
                Key.O,
                Key.P,
                Key.Oem4, //[
                Key.Oem5, //]
            },
            {
                Key.H,
                Key.J,
                Key.K,
                Key.L,
                Key.Oem7, //;
                Key.Oem8, //'
                Key.Invalid,
            },
            {
                Key.B,
                Key.N,
                Key.M,
                Key.Oem9, //,
                Key.Oem10, //.
                Key.Oem11, ///
                Key.Invalid,
            },
        };

        private class PlayerData
        {
            public int _mScore = 0;
            public int _mPosition = 1; //0, 1, 2
        }

        private static PlayerData _sPlayer1 = new PlayerData();
        private static PlayerData _sPlayer2 = new PlayerData();
        private static BallData _sBall = new BallData();

        private static Random _sRandom = new Random(123);

        private static DateTime _sTimerPlayer1Block = DateTime.MinValue;
        private static DateTime _sTimerPlayer2Block = DateTime.MinValue;

        private static DateTime _sTimerPlayer1Score = DateTime.MinValue;
        private static DateTime _sTimerPlayer2Score = DateTime.MinValue;

        public Form1()
        {
            InitializeComponent();
        }

        private void _mButtonQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static void UpdateBall()
        {
            for (int i = 0; i < _sKeys.GetLength(0); ++i)
            {
                for (int j = 0; j < _sKeys.GetLength(1); ++j)
                {
                    KeyData keyData = _sKeys[i, j];
                    if (i == _sBall.GetRow() &&
                        j == _sBall.GetColumn())
                    {
                        keyData._mColor = Color.White;
                    }
                    else
                    {
                        keyData._mColor = Color.Green;
                    }
                    SetColor(keyData._mKey, keyData._mColor);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _mKeyboardEvents = Hook.GlobalEvents();
            _mKeyboardEvents.KeyDown += HandleKeyDown;

            UpdatePlayer1Color();
            UpdatePlayer2Color();
            UpdateBall();

            timerPlay.Start();
        }

        private static void SetColor(Key key, Color color)
        {
            if (key != Key.Invalid)
            {
                Keyboard.Instance.SetKey(key, color);
            }
        }

        private static Color GetActiveColor(bool active, Color activeColor, Color inactiveColor)
        {
            return active ? activeColor : inactiveColor;
        }

        private static void UpdatePlayer1Color()
        {
            Color activeColor = Color.Red;
            Color inactiveColor = Color.Black;
            if (_sTimerPlayer1Block > DateTime.Now)
            {
                activeColor = COLOR_BLOCK;
                inactiveColor = activeColor;
            }
            if (_sTimerPlayer1Score > DateTime.Now)
            {
                activeColor = COLOR_SCORE_P1;
                inactiveColor = activeColor;
            }
            SetColor(Key.Tab, GetActiveColor(_sPlayer1._mPosition == 0, activeColor, inactiveColor));
            SetColor(Key.CapsLock, GetActiveColor(_sPlayer1._mPosition == 1, activeColor, inactiveColor));
            SetColor(Key.LeftShift, GetActiveColor(_sPlayer1._mPosition == 2, activeColor, inactiveColor));
        }

        private static void UpdatePlayer2Color()
        {
            Color activeColor = Color.Red;
            Color inactiveColor = Color.Black;
            if (_sTimerPlayer2Block > DateTime.Now)
            {
                activeColor = COLOR_BLOCK;
                inactiveColor = activeColor;
            }
            if (_sTimerPlayer2Score > DateTime.Now)
            {
                activeColor = COLOR_SCORE_P2;
                inactiveColor = activeColor;
            }
            SetColor(Key.Oem6, GetActiveColor(_sPlayer2._mPosition == 0, activeColor, inactiveColor));
            SetColor(Key.Enter, GetActiveColor(_sPlayer2._mPosition == 1, activeColor, inactiveColor));
            SetColor(Key.RightShift, GetActiveColor(_sPlayer2._mPosition == 2, activeColor, inactiveColor));
        }

        private static void UpdateScore()
        {
            Color activeColor1 = COLOR_SCORE_P2;
            Color activeColor2 = COLOR_SCORE_P1;
            Color inactiveColor = Color.Black;

            SetColor(Key.One, GetActiveColor(_sPlayer1._mScore > 0, activeColor1, inactiveColor));
            SetColor(Key.Two, GetActiveColor(_sPlayer1._mScore > 1, activeColor1, inactiveColor));
            SetColor(Key.Three, GetActiveColor(_sPlayer1._mScore > 2, activeColor1, inactiveColor));
            SetColor(Key.Four, GetActiveColor(_sPlayer1._mScore > 3, activeColor1, inactiveColor));
            SetColor(Key.Five, GetActiveColor(_sPlayer1._mScore > 4, activeColor1, inactiveColor));
            SetColor(Key.Six, GetActiveColor(_sPlayer1._mScore > 5, activeColor1, inactiveColor));
            SetColor(Key.Seven, GetActiveColor(_sPlayer1._mScore > 6, activeColor1, inactiveColor));
            SetColor(Key.Eight, GetActiveColor(_sPlayer1._mScore > 7, activeColor1, inactiveColor));
            SetColor(Key.Nine, GetActiveColor(_sPlayer1._mScore > 8, activeColor1, inactiveColor));
            SetColor(Key.Zero, GetActiveColor(_sPlayer1._mScore > 9, activeColor1, inactiveColor));

            SetColor(Key.F10, GetActiveColor(_sPlayer2._mScore > 0, activeColor2, inactiveColor));
            SetColor(Key.F9, GetActiveColor(_sPlayer2._mScore > 1, activeColor2, inactiveColor));
            SetColor(Key.F8, GetActiveColor(_sPlayer2._mScore > 2, activeColor2, inactiveColor));
            SetColor(Key.F7, GetActiveColor(_sPlayer2._mScore > 3, activeColor2, inactiveColor));
            SetColor(Key.F6, GetActiveColor(_sPlayer2._mScore > 4, activeColor2, inactiveColor));
            SetColor(Key.F5, GetActiveColor(_sPlayer2._mScore > 5, activeColor2, inactiveColor));
            SetColor(Key.F4, GetActiveColor(_sPlayer2._mScore > 6, activeColor2, inactiveColor));
            SetColor(Key.F3, GetActiveColor(_sPlayer2._mScore > 7, activeColor2, inactiveColor));
            SetColor(Key.F2, GetActiveColor(_sPlayer2._mScore > 8, activeColor2, inactiveColor));
            SetColor(Key.F1, GetActiveColor(_sPlayer2._mScore > 9, activeColor2, inactiveColor));
        }

        private static void ProcessInputPlayer(Key key, KeyData[,] keys, PlayerData playerData)
        {
            for (int i = 0; i < keys.GetLength(0); ++i)
            {
                for (int j = 0; j < keys.GetLength(1); ++j)
                {
                    KeyData keyData = keys[i, j];
                    if (keyData._mKey == Key.Invalid)
                    {
                        continue;
                    }
                    if (keyData._mKey == key)
                    {
                        playerData._mPosition = i;
                        UpdatePlayer1Color();
                        UpdatePlayer2Color();
                        return;
                    }
                }
            }
        }

        private static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            Key key;
            string strKey = e.KeyCode.ToString();
            if (!Enum.TryParse<Key>(strKey, true, out key))
            {
                return; //no-op
            }
            ProcessInputPlayer(key, _sPlayer1Keys, _sPlayer1);
            ProcessInputPlayer(key, _sPlayer2Keys, _sPlayer2);
        }

        private static bool IsPlayer1Blocking()
        {
            if (_sPlayer1._mPosition == (int)_sBall._mPosition._mY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsPlayer2Blocking()
        {
            if (_sPlayer2._mPosition == (int)_sBall._mPosition._mY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void timerPlay_Tick(object sender, EventArgs e)
        {
            _sBall._mPosition = _sBall._mPosition + _sBall._mDirection * BALL_SPEED;
            if (_sBall._mPosition._mY < 0f)
            {
                _sBall._mPosition._mY = 0f;
                _sBall._mDirection._mY = -_sBall._mDirection._mY;
            }
            else if (_sBall._mPosition._mY > 2f)
            {
                _sBall._mPosition._mY = 2f;
                _sBall._mDirection._mY = -_sBall._mDirection._mY;
            }

            if (_sBall.GetColumn() < _sKeys.GetLength(1))
            {
            }
            else
            {
                if (IsPlayer2Blocking())
                {
                    _sTimerPlayer2Block = DateTime.Now + TimeSpan.FromSeconds(1);
                }
                else
                {
                    _sTimerPlayer2Score = DateTime.Now + TimeSpan.FromSeconds(1);
                    ++_sPlayer1._mScore;
                }

                _sBall._mDirection = -Vector2.Right + new Vector2(0, (float)_sRandom.NextDouble());
                _sBall._mPosition = new Vector2(_sKeys.GetLength(1) - 1, _sPlayer2._mPosition);
            }

            if (_sBall.GetColumn() >= 0)
            {
            }
            else
            {
                if (IsPlayer1Blocking())
                {
                    _sTimerPlayer1Block = DateTime.Now + TimeSpan.FromSeconds(1);
                }
                else
                {
                    _sTimerPlayer1Score = DateTime.Now + TimeSpan.FromSeconds(1);
                    ++_sPlayer2._mScore;
                }

                _sBall._mDirection = Vector2.Right + new Vector2(0, (float)_sRandom.NextDouble());
                _sBall._mPosition = new Vector2(1, _sPlayer1._mPosition);
            }

            if (_sPlayer1._mScore >= 10 ||
                _sPlayer2._mScore >= 10)
            {
                _sPlayer1._mScore = 0;
                _sPlayer2._mScore = 0;
            }

            UpdateBall();
            UpdateScore();
            UpdatePlayer1Color();
            UpdatePlayer2Color();
        }
    }
}
