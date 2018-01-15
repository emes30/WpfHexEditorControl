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
    internal class HexByte : BaseByte, IByteControl
    {
        #region Global class variables

        private KeyDownLabel _keyDownLabel = KeyDownLabel.FirstChar;

        #endregion global class variables

        #region Constructor

        public HexByte(HexEditor parent) : base(parent)
        {
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

        #region Methods


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
            if (_isAutoHighlight)
                dc.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Red, .5), new Rect(0, 0, RenderSize.Width, RenderSize.Height));

            //Draw text
            var typeface = new Typeface(_parent.FontFamily, _parent.FontStyle, FontWeight, _parent.FontStretch);
            var formatedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, _parent.FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formatedText, new Point(2, 0));
        }

        protected override void UpdateLabelFromByte()
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

        public override void Clear()
        {
            base.Clear();
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
            }

            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Byte == null) return;

            if (KeyValidation(e)) return;

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
                                    OnMoveNext(new EventArgs());
                                }
                                break;
                            case KeyDownLabel.NextPosition:

                                //byte[] byteToAppend = { (byte)key.ToCharArray()[0] };
                                _parent.AppendByte(new byte[] { 0 });

                                OnMoveNext(new EventArgs());

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