using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Documents;
using WpfHexaEditor.Core;
using System.Windows.Controls;

namespace WpfHexaEditor
{
    abstract class BaseByte : FrameworkElement
    {
        #region Global class variables

        protected readonly HexEditor _parent;
        protected bool _isSelected;
        protected Brush _highlightBrush;
        protected bool _isMouseOver;
        protected bool _isAutoHighlight;
        protected ByteAction _action = ByteAction.Nothing;
        protected byte? _byte;

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

        public BaseByte(HexEditor parent)
        {
            //Parent hexeditor
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            //Default properties
            DataContext = this;
            Focusable = true;
        }

        #endregion

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
            get => (_highlightBrush != null);
            set
            {
                //    if (value == _isHighLight) return;

                //    _isHighLight = value;
                //    _keyDownLabel = KeyDownLabel.FirstChar;
                //    UpdateVisual();
            }
        }

        /// <summary>
        /// Get or set Highlight Brush
        /// </summary>
        public Brush HighlightBrush
        {
            get => _highlightBrush;
            set
            {
                _highlightBrush = value;
                UpdateVisual();
            }
        }

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

        #endregion

        #region Private base properties

        /// <summary>
        /// Definie the foreground
        /// </summary>
        private static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(BaseByte));

        protected Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static readonly DependencyProperty BackgroundProperty =
            TextElement.BackgroundProperty.AddOwner(typeof(BaseByte),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Defines the background
        /// </summary>
        protected Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        private static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(BaseByte),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Text to be displayed representation of Byte
        /// </summary>
        protected string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(BaseByte));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        protected FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion Base properties

        #region Methods

        /// <summary>
        /// Update Background,foreground and font property
        /// </summary>
        public virtual void UpdateVisual()
        {
            if (IsSelected)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.ForegroundContrast;

                Background = FirstSelected ? _parent.SelectionFirstColor : _parent.SelectionSecondColor;
            }
            else if (_highlightBrush != null)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.Foreground;
                Background = _highlightBrush;
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

            InvalidateVisual();
        }

        protected void UpdateAutoHighLiteSelectionByteVisual()
        {
            _isAutoHighlight = false;

            //Auto highlite selectionbyte
            if (_parent.AllowAutoHightLighSelectionByte && _parent.SelectionByte != null &&
                Byte == _parent.SelectionByte && !IsSelected)
            {
                //if (!IsHighLight)
                //    Background = _parent.AutoHighLiteSelectionByteBrush;

                _isAutoHighlight = true;
            }
        }

        protected abstract void UpdateLabelFromByte();

        /// <summary>
        /// Clear control
        /// </summary>
        public virtual void Clear()
        {
            InternalChange = true;
            BytePositionInFile = -1;
            Byte = null;
            Action = ByteAction.Nothing;
            _highlightBrush = null;
            IsSelected = false;
            InternalChange = false;
        }

        #endregion

        #region Events delegate

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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsFocused)
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

        protected void OnMoveNext(EventArgs e)
        {
            MoveNext?.Invoke(this, e);
        }

        protected bool KeyValidation(KeyEventArgs e)
        {
            #region Key validation and launch event if needed

            if (KeyValidator.IsUpKey(e.Key))
            {
                e.Handled = true;
                MoveUp?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsDownKey(e.Key))
            {
                e.Handled = true;
                MoveDown?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsLeftKey(e.Key))
            {
                e.Handled = true;
                MoveLeft?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsRightKey(e.Key))
            {
                e.Handled = true;
                MoveRight?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsPageDownKey(e.Key))
            {
                e.Handled = true;
                MovePageDown?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsPageUpKey(e.Key))
            {
                e.Handled = true;
                MovePageUp?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsDeleteKey(e.Key))
            {
                if (!ReadOnlyMode)
                {
                    e.Handled = true;
                    ByteDeleted?.Invoke(this, new EventArgs());

                    return true;
                }
            }
            else if (KeyValidator.IsBackspaceKey(e.Key))
            {
                e.Handled = true;
                ByteDeleted?.Invoke(this, new EventArgs());

                MovePrevious?.Invoke(this, new EventArgs());

                return true;
            }
            else if (KeyValidator.IsEscapeKey(e.Key))
            {
                e.Handled = true;
                EscapeKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlZKey(e.Key))
            {
                e.Handled = true;
                CtrlzKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlVKey(e.Key))
            {
                e.Handled = true;
                CtrlvKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlCKey(e.Key))
            {
                e.Handled = true;
                CtrlcKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlAKey(e.Key))
            {
                e.Handled = true;
                CtrlaKey?.Invoke(this, new EventArgs());
                return true;
            }

            return false;
            #endregion
        }
    }
    #endregion

}

