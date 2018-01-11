//////////////////////////////////////////////
// Apache 2.0  - 2016-2017
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributor: Janus Tida
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Core.Interfaces;
using WpfHexaEditor.Core.MethodExtention;

namespace WpfHexaEditor
{
    internal class HexByte : FrameworkElement, IByteControl
    {
        #region Global class variables

        private KeyDownLabel _keyDownLabel = KeyDownLabel.FirstChar;
        private readonly HexEditor _parent;
        private bool _isSelected;
        private bool _isHighLight;
        private bool _isMouseOver;
        private ByteAction _action = ByteAction.Nothing;
        private byte? _byte;

        #endregion global class variables

        #region Events

        public event EventHandler ByteModified;
        public event EventHandler MouseSelection;
        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler MoveNext;
        public event EventHandler MovePrevious;
        public event EventHandler MoveRight;
        public event EventHandler MoveLeft;
        public event EventHandler MoveUp;
        public event EventHandler MoveDown;
        public event EventHandler MovePageDown;
        public event EventHandler MovePageUp;
        public event EventHandler ByteDeleted;
        public event EventHandler EscapeKey;
        public event EventHandler CtrlzKey;
        public event EventHandler CtrlvKey;
        public event EventHandler CtrlcKey;
        public event EventHandler CtrlaKey;

        #endregion Events

        #region Constructor

        public HexByte(HexEditor parent)
        {
            //Parent hexeditor
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            //Default properties
            DataContext = this;
            Focusable = true;

            #region Binding tooltip

            LoadDictionary("/WPFHexaEditor;component/Resources/Dictionary/ToolTipDictionary.xaml");
            var txtBinding = new Binding
            {
                Source = FindResource("ByteToolTip"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            };

            // Load ressources dictionnary
            void LoadDictionary(string url)
            {
                var ttRes = new ResourceDictionary { Source = new Uri(url, UriKind.Relative) };
                Resources.MergedDictionaries.Add(ttRes);
            }

            SetBinding(ToolTipProperty, txtBinding);

            #endregion

            //Update width
            UpdateDataVisualWidth();
        }

        #endregion Contructor

        #region Properties

        /// <summary>
        /// Position in file
        /// </summary>
        public long BytePositionInFile { get; set; } = -1L;

        /// <summary>
        /// Action with this byte
        /// </summary>
        public ByteAction Action
        {
            get => _action;
            set
            {
                _action = value != ByteAction.All ? value : ByteAction.Nothing;

                UpdateVisual();
            }
        }

        /// <summary>
        /// Used for selection coloring
        /// </summary>
        public bool FirstSelected { get; set; }

        /// <summary>
        /// Byte used for this instance
        /// </summary>
        public byte? Byte
        {
            get => _byte;
            set
            {
                _byte = value;

                if (Action != ByteAction.Nothing && InternalChange == false)
                    ByteModified?.Invoke(this, new EventArgs());

                UpdateLabelFromByte();
            }
        }

        /// <summary>
        /// Used to prevent ByteModified event occurc when we dont want! 
        /// </summary>
        public bool InternalChange { get; set; }

        /// <summary>
        /// Get or set if control as in read only mode
        /// </summary>
        public bool ReadOnlyMode { get; set; }

        /// <summary>
        /// Get or Set if control as selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;

                _isSelected = value;
                UpdateVisual();
            }
        }

        /// <summary>
        /// Get of Set if control as marked as highlighted
        /// </summary>   
        public bool IsHighLight
        {
            get => _isHighLight;
            set
            {
                if (value == _isHighLight) return;

                _isHighLight = value;
                _keyDownLabel = KeyDownLabel.FirstChar;
                UpdateVisual();
            }
        }

        #endregion properties

        #region Private base properties

        /// <summary>
        /// Definie the foreground
        /// </summary>
        private static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(HexByte));

        private Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static readonly DependencyProperty BackgroundProperty =
            TextElement.BackgroundProperty.AddOwner(typeof(HexByte),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Defines the background
        /// </summary>
        private Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        private static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(HexByte),
                new FrameworkPropertyMetadata(string.Empty, 
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Text to be displayed representation of Byte
        /// </summary>
        private string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(HexByte));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        private FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion Base properties

        #region Methods

        /// <summary>
        /// Update Background,foreground and font property
        /// </summary>
        public void UpdateVisual()
        {
            if (IsSelected)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.ForegroundContrast;

                Background = FirstSelected ? _parent.SelectionFirstColor : _parent.SelectionSecondColor;
            }
            else if (IsHighLight)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.Foreground;
                Background = _parent.HighLightColor;
            }
            else if (Action != ByteAction.Nothing)
            {
                FontWeight = FontWeights.Bold;
                Foreground = _parent.Foreground;

                switch (Action)
                {
                    case ByteAction.Modified:
                        Background = _parent.ByteModifiedColor;
                        break;
                    case ByteAction.Deleted:
                        Background = _parent.ByteDeletedColor;
                        break;
                }
            }
            else
            {
                FontWeight = _parent.FontWeight;
                Background = Brushes.Transparent;
                Foreground = _parent.GetColumnNumber(BytePositionInFile) % 2 == 0 ? _parent.Foreground : _parent.ForegroundSecondColor;
                //Foreground = BytePositionInFile % 2 == 0 ? _parent.Foreground : _parent.ForegroundSecondColor;
            }

            UpdateAutoHighLiteSelectionByteVisual();
        }

        /// <summary>
        /// Render the control
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            //Draw background
            if (Background != null)
                dc.DrawRectangle(Background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            if (_isMouseOver)
                dc.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Lime, 1), new Rect(0, 0, RenderSize.Width, RenderSize.Height));

                //Draw text
                var typeface = new Typeface(_parent.FontFamily, _parent.FontStyle, FontWeight, _parent.FontStretch);
            var formatedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, _parent.FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formatedText, new Point(2, 0));
        }

        private void UpdateAutoHighLiteSelectionByteVisual()
        {
            //Auto highlite selectionbyte
            if (_parent.AllowAutoHightLighSelectionByte && _parent.SelectionByte != null &&
                Byte == _parent.SelectionByte && !IsSelected)
                Background = _parent.AutoHighLiteSelectionByteBrush;
        }

        internal void UpdateLabelFromByte()
        {
            if (Byte != null)
            {
                switch (_parent.DataStringVisual)
                {
                    case DataVisualType.Hexadecimal:
                        var chArr = ByteConverters.ByteToHexCharArray(Byte.Value);
                        Text = new string(chArr);
                        break;
                    case DataVisualType.Decimal:
                        Text = Byte.Value.ToString("d3");
                        break;
                }
            }
            else
                Text = string.Empty;
        }

        /// <summary>
        /// Clear control
        /// </summary>
        public void Clear()
        {
            InternalChange = true;
            BytePositionInFile = -1;
            Byte = null;
            Action = ByteAction.Nothing;
            IsHighLight = false;
            IsSelected = false;
            InternalChange = false;
            _keyDownLabel = KeyDownLabel.FirstChar;
        }

        public void UpdateDataVisualWidth()
        {
            switch (_parent.DataStringVisual)
            {
                case DataVisualType.Decimal:
                    Width = 25;
                    break;
                case DataVisualType.Hexadecimal:
                    Width = 20;
                    break;
            }
        }

        #endregion Methods

        #region Events delegate

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsFocused)
                {
                    //Is focused set editing to second char.
                    _keyDownLabel = KeyDownLabel.SecondChar;
                    UpdateCaret();
                }
                else
                    Focus();

                Click?.Invoke(this, e);
            }

            if (e.RightButton == MouseButtonState.Pressed)
                RightClick?.Invoke(this, e);

            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Byte == null) return;

            #region Key validation and launch event if needed

            if (KeyValidator.IsUpKey(e.Key))
            {
                e.Handled = true;
                MoveUp?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsDownKey(e.Key))
            {
                e.Handled = true;
                MoveDown?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsLeftKey(e.Key))
            {
                e.Handled = true;
                MoveLeft?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsRightKey(e.Key))
            {
                e.Handled = true;
                MoveRight?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsPageDownKey(e.Key))
            {
                e.Handled = true;
                MovePageDown?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsPageUpKey(e.Key))
            {
                e.Handled = true;
                MovePageUp?.Invoke(this, new EventArgs());

                return;
            }
            if (KeyValidator.IsDeleteKey(e.Key))
            {
                if (!ReadOnlyMode)
                {
                    e.Handled = true;
                    ByteDeleted?.Invoke(this, new EventArgs());

                    return;
                }
            }
            else if (KeyValidator.IsBackspaceKey(e.Key))
            {
                e.Handled = true;
                ByteDeleted?.Invoke(this, new EventArgs());

                MovePrevious?.Invoke(this, new EventArgs());

                return;
            }
            else if (KeyValidator.IsEscapeKey(e.Key))
            {
                e.Handled = true;
                EscapeKey?.Invoke(this, new EventArgs());
                return;
            }
            else if (KeyValidator.IsCtrlZKey(e.Key))
            {
                e.Handled = true;
                CtrlzKey?.Invoke(this, new EventArgs());
                return;
            }
            else if (KeyValidator.IsCtrlVKey(e.Key))
            {
                e.Handled = true;
                CtrlvKey?.Invoke(this, new EventArgs());
                return;
            }
            else if (KeyValidator.IsCtrlCKey(e.Key))
            {
                e.Handled = true;
                CtrlcKey?.Invoke(this, new EventArgs());
                return;
            }
            else if (KeyValidator.IsCtrlAKey(e.Key))
            {
                e.Handled = true;
                CtrlaKey?.Invoke(this, new EventArgs());
                return;
            }

            #endregion

            //MODIFY BYTE
            if (!ReadOnlyMode && KeyValidator.IsHexKey(e.Key))
                switch (_parent.DataStringVisual)
                {
                    case DataVisualType.Hexadecimal:

                        #region Edit hexadecimal value 

                        string key;
                        key = KeyValidator.IsNumericKey(e.Key)
                            ? KeyValidator.GetDigitFromKey(e.Key).ToString()
                            : e.Key.ToString().ToLower();

                        //Update byte
                        var byteValueCharArray = ByteConverters.ByteToHexCharArray(Byte.Value);
                        switch (_keyDownLabel)
                        {
                            case KeyDownLabel.FirstChar:
                                byteValueCharArray[0] = key.ToCharArray()[0];
                                _keyDownLabel = KeyDownLabel.SecondChar;
                                Action = ByteAction.Modified;
                                Byte = ByteConverters.HexToByte(
                                    byteValueCharArray[0] + byteValueCharArray[1].ToString())[0];
                                break;
                            case KeyDownLabel.SecondChar:
                                byteValueCharArray[1] = key.ToCharArray()[0];
                                _keyDownLabel = KeyDownLabel.NextPosition;

                                Action = ByteAction.Modified;
                                Byte = ByteConverters.HexToByte(
                                    byteValueCharArray[0] + byteValueCharArray[1].ToString())[0];

                                //Insert byte at end of file
                                if (_parent.Lenght != BytePositionInFile + 1)
                                {
                                    _keyDownLabel = KeyDownLabel.NextPosition;
                                    MoveNext?.Invoke(this, new EventArgs());
                                }
                                break;
                            case KeyDownLabel.NextPosition:

                                //byte[] byteToAppend = { (byte)key.ToCharArray()[0] };
                                _parent.AppendByte(new byte[] { 0 });

                                MoveNext?.Invoke(this, new EventArgs());

                                break;
                        }

                        #endregion

                        break;
                    case DataVisualType.Decimal:

                        //Not editable at this moment, maybe in future

                        break;
                }

            UpdateCaret();

            base.OnKeyDown(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (Byte != null) // && Action != ByteAction.Modified && Action != ByteAction.Deleted &&
                              //Action != ByteAction.Added && !IsSelected && !IsHighLight)
            {
                Background = _parent.MouseOverColor;
                _isMouseOver = true;
            }

            UpdateAutoHighLiteSelectionByteVisual();

            if (e.LeftButton == MouseButtonState.Pressed)
                MouseSelection?.Invoke(this, e);

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {

            if (Byte != null) // && Action != ByteAction.Modified && Action != ByteAction.Deleted &&
                              //Action != ByteAction.Added && !IsSelected && !IsHighLight)
            {
                Background = Brushes.Transparent;
                _isMouseOver = false;
                UpdateVisual();
            }

            UpdateAutoHighLiteSelectionByteVisual();

            base.OnMouseLeave(e);
        }

        protected override void OnToolTipOpening(ToolTipEventArgs e)
        {
            if (Byte == null)
                e.Handled = true;

            base.OnToolTipOpening(e);
        }

        #endregion Events delegate

        #region Caret events/methods

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            _parent.HideCaret();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            _keyDownLabel = KeyDownLabel.FirstChar;
            UpdateCaret();

            base.OnGotFocus(e);
        }

        //private void UserControl_GotFocus(object sender, RoutedEventArgs e) => UpdateCaret();

        private void UpdateCaret()
        {
            if (ReadOnlyMode || Byte == null)
                _parent.HideCaret();
            else
            {
                var size = Text[1].ToString()
                    .GetScreenSize(_parent.FontFamily, _parent.FontSize, _parent.FontStyle, FontWeight,
                        _parent.FontStretch);

                _parent.SetCaretSize(Width / 2, size.Height);

                switch (_keyDownLabel)
                {
                    case KeyDownLabel.FirstChar:
                        _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(0, 0)));
                        break;
                    case KeyDownLabel.SecondChar:
                        _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(size.Width + 1, 0)));
                        break;
                    case KeyDownLabel.NextPosition:
                        if (_parent.Lenght == BytePositionInFile + 1)
                            _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(size.Width * 2, 0)));
                        break;
                }
            }
        }

        #endregion

    }
}