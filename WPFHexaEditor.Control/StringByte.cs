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
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Core.Interfaces;

namespace WpfHexaEditor
{
    internal class StringByte : FrameworkElement, IByteControl
    {
        #region Global class variables

        private readonly HexEditor _parent;
        private bool _isSelected;
        private bool _isHighLight;
        private ByteAction _action = ByteAction.Nothing;
        private byte? _byte;
        private bool _tblShowMte = true;

        #endregion Global variable

        #region Events

        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler MouseSelection;
        public event EventHandler ByteModified;
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

        #region Contructor

        public StringByte(HexEditor parent)
        {
            //Parent hexeditor
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            //Default properties
            Width = 10;
            Focusable = true;
            DataContext = this;

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
                var ttRes = new ResourceDictionary {Source = new Uri(url, UriKind.Relative)};
                Resources.MergedDictionaries.Add(ttRes);
            }

            SetBinding(ToolTipProperty, txtBinding);

            #endregion
        }
        #endregion Contructor

        #region Properties

        /// <summary>
        /// Position in file
        /// </summary>
        public long BytePositionInFile { get; set; } = -1L;

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
        /// Next Byte of this instance (used for TBL/MTE decoding)
        /// </summary>
        public byte? ByteNext { get; set; }

        /// <summary>
        /// Get or set if control as selected
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
                UpdateVisual();
            }
        }

        /// <summary>
        /// Get or set if control as in read only mode
        /// </summary>
        public bool ReadOnlyMode { get; set; }

        /// <summary>
        /// Used to prevent StringByte event occur when we dont want!
        /// </summary>
        public bool InternalChange { get; set; }

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

        #endregion Properties
        
        #region Private base properties

        /// <summary>
        /// Definie the foreground
        /// </summary>
        private static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(StringByte));

        private Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static readonly DependencyProperty BackgroundProperty =
            TextElement.BackgroundProperty.AddOwner(typeof(StringByte),
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
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(StringByte),
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

        private static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(StringByte));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        private FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion Base properties

        #region Characters tables

        /// <summary>
        /// Show or not Multi Title Enconding (MTE) are loaded in TBL file
        /// </summary>
        public bool TblShowMte
        {
            get => _tblShowMte;
            set
            {
                _tblShowMte = value;
                UpdateLabelFromByte();
            }
        }

        /// <summary>
        /// Type of caracter table are used un hexacontrol.
        /// For now, somes character table can be readonly but will change in future
        /// </summary>
        public CharacterTableType TypeOfCharacterTable { get; set; }

        /// <summary>
        /// Custom character table
        /// </summary>
        public TblStream TblCharacterTable { get; set; }

        #endregion Characters tables

        #region Methods

        /// <summary>
        /// Update control label from byte property
        /// </summary>
        private void UpdateLabelFromByte()
        {
            if (Byte != null)
            {
                switch (TypeOfCharacterTable)
                {
                    case CharacterTableType.Ascii:
                        Text = ByteConverters.ByteToChar(Byte.Value).ToString();
                        break;
                    case CharacterTableType.TblFile:
                        if (TblCharacterTable != null)
                        {
                            ReadOnlyMode = !TblCharacterTable.AllowEdit;

                            var content = "#";

                            if (TblShowMte && ByteNext.HasValue)
                            {
                                var mte = ByteConverters.ByteToHex(Byte.Value) +
                                          ByteConverters.ByteToHex(ByteNext.Value);
                                content = TblCharacterTable.FindMatch(mte, true);
                            }

                            if (content == "#")
                                content = TblCharacterTable.FindMatch(ByteConverters.ByteToHex(Byte.Value), true);

                            Text = content;
                        }
                        else
                            goto case CharacterTableType.Ascii;
                        break;
                }
            }
            else
                Text = string.Empty;
        }
        
        /// <summary>
        /// Update Background,foreground and font property
        /// </summary>
        public void UpdateVisual()
        {
            //FontFamily = _parent.FontFamily;

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
                switch (Action)
                {
                    case ByteAction.Modified:
                        FontWeight = FontWeights.Bold;
                        Background = _parent.ByteModifiedColor;
                        Foreground = _parent.Foreground;
                        break;

                    case ByteAction.Deleted:
                        FontWeight = FontWeights.Bold;
                        Background = _parent.ByteDeletedColor;
                        Foreground = _parent.Foreground;
                        break;
                }
            }
            else
            {
                #region TBL COLORING

                FontWeight = _parent.FontWeight;
                Background = Brushes.Transparent;
                Foreground = _parent.Foreground;

                if (TypeOfCharacterTable == CharacterTableType.TblFile)
                    switch (Dte.TypeDte(Text))
                    {
                        case DteType.DualTitleEncoding:
                            Foreground = _parent.TbldteColor;
                            break;
                        case DteType.MultipleTitleEncoding:
                            Foreground = _parent.TblmteColor;
                            break;
                        case DteType.EndLine:
                            Foreground = _parent.TblEndLineColor;
                            break;
                        case DteType.EndBlock:
                            Foreground = _parent.TblEndBlockColor;
                            break;
                        default:
                            Foreground = _parent.TblDefaultColor;
                            break;
                    }

                #endregion
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

            //Draw text
            var typeface = new Typeface(_parent.FontFamily, _parent.FontStyle, FontWeight, _parent.FontStretch);
            var ft = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface,
                _parent.FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(ft, new Point(0, 0));

            #region Update width of control 
            //It's 8-10 time more fastest to update width on render for TBL string
            switch (TypeOfCharacterTable)
            {
                case CharacterTableType.Ascii:
                    Width = 12;
                    break;
                case CharacterTableType.TblFile:
                    Width = ft.Width > 12 ? ft.Width : 12;
                    break;
            }
            #endregion
        }

        private void UpdateAutoHighLiteSelectionByteVisual()
        {
            //Auto highlite selectionbyte
            if (_parent.AllowAutoHightLighSelectionByte && _parent.SelectionByte != null &&
                Byte == _parent.SelectionByte && !IsSelected)
                Background = _parent.AutoHighLiteSelectionByteBrush;
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
            ByteNext = null;
            InternalChange = false;
        }

        #endregion Methods

        #region Events delegate

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Byte == null) return;

            #region Key validation and launch event if needed

            if (KeyValidator.IsIgnoredKey(e.Key))
            {
                e.Handled = true;
                return;
            }
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
                if (!ReadOnlyMode)
                {
                    e.Handled = true;
                    ByteDeleted?.Invoke(this, new EventArgs());

                    if (BytePositionInFile > 0)
                        MovePrevious?.Invoke(this, new EventArgs());

                    return;
                }
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

            //MODIFY ASCII...
            if (!ReadOnlyMode)
            {
                var isok = false;

                if (Keyboard.GetKeyStates(Key.CapsLock) == KeyStates.Toggled)
                {
                    if (Keyboard.Modifiers != ModifierKeys.Shift && e.Key != Key.RightShift && e.Key != Key.LeftShift)
                    {
                        Text = KeyValidator.GetCharFromKey(e.Key).ToString();
                        isok = true;
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key != Key.RightShift &&
                             e.Key != Key.LeftShift)
                    {
                        isok = true;
                        Text = KeyValidator.GetCharFromKey(e.Key).ToString().ToLower();
                    }
                }
                else
                {
                    if (Keyboard.Modifiers != ModifierKeys.Shift && e.Key != Key.RightShift && e.Key != Key.LeftShift)
                    {
                        Text = KeyValidator.GetCharFromKey(e.Key).ToString().ToLower();
                        isok = true;
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key != Key.RightShift &&
                             e.Key != Key.LeftShift)
                    {
                        isok = true;
                        Text = KeyValidator.GetCharFromKey(e.Key).ToString();
                    }
                }

                //Move focus event
                if (MoveNext != null && isok)
                {
                    Action = ByteAction.Modified;
                    Byte = ByteConverters.CharToByte(Text[0]);

                    //Insert byte at end of file
                    if (_parent.Lenght == BytePositionInFile + 1)
                    {
                        byte[] byteToAppend = {0};
                        _parent.AppendByte(byteToAppend);
                    }

                    MoveNext(this, new EventArgs());
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (Byte != null && Action != ByteAction.Modified && Action != ByteAction.Deleted &&
                Action != ByteAction.Added && !IsSelected && !IsHighLight)
                Background = _parent.MouseOverColor;

            UpdateAutoHighLiteSelectionByteVisual();

            if (e.LeftButton == MouseButtonState.Pressed)
                MouseSelection?.Invoke(this, e);

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (Byte != null && Action != ByteAction.Modified && Action != ByteAction.Deleted &&
                Action != ByteAction.Added && !IsSelected && !IsHighLight)
                Background = Brushes.Transparent;

            UpdateAutoHighLiteSelectionByteVisual();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Focus();
                Click?.Invoke(this, e);
            }

            if (e.RightButton == MouseButtonState.Pressed)
                RightClick?.Invoke(this, e);

            base.OnMouseDown(e);
        }

        protected override void OnToolTipOpening(ToolTipEventArgs e)
        {
            if (Byte == null)
                e.Handled = true;

            base.OnToolTipOpening(e);
        }

        #endregion Events delegate

        #region Caret events

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            _parent.HideCaret();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            if (ReadOnlyMode || Byte == null)
                _parent.HideCaret();
            else
                _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(0, 0)));

            base.OnGotFocus(e);
        }
        #endregion
    }
}