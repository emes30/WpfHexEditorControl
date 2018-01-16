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
    internal class StringByte : BaseByte, IByteControl
    {
        #region Global class variables

        private bool _tblShowMte = true;

        #endregion Global variable

        #region Contructor

        public StringByte(HexEditor parent) : base(parent)
        {
            //Default properties
            Width = 10;

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
        /// Next Byte of this instance (used for TBL/MTE decoding)
        /// </summary>
        public byte? ByteNext { get; set; }

        #endregion Properties
        
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
        protected override void UpdateLabelFromByte()
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
        public override void UpdateVisual()
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
                Background = _highlightBrush;
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

        /// <summary>
        /// Clear control
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            ByteNext = null;
        }

        #endregion Methods

        #region Events delegate

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Byte == null) return;

            if (KeyValidation(e)) return;

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
                if (isok)
                {
                    Action = ByteAction.Modified;
                    Byte = ByteConverters.CharToByte(Text[0]);

                    //Insert byte at end of file
                    if (_parent.Lenght == BytePositionInFile + 1)
                    {
                        byte[] byteToAppend = {0};
                        _parent.AppendByte(byteToAppend);
                    }

                    OnMoveNext(new EventArgs());
                }
            }

            base.OnKeyDown(e);
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
