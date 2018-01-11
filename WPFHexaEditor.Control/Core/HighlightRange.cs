using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfHexaEditor.Core
{
    public class HighlightRange
    {
        public long StartPosition { get; set; }
        public long EndPosition { get; set; }
        public Brush HighlightBrush { get; set; }
    }

    public class HighlightRangeList : List<HighlightRange>
    {
        public Brush GetHighlightBrush(long position)
        {
            return this.Where(h => h.StartPosition <= position && h.EndPosition >= position).LastOrDefault()?.HighlightBrush;
        }
    }
}
