using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveEye.Events
{
    public class RaiseToolTipEventArgs : EventArgs
    {
        public string Text { get; set; }
        public int DisplayDuration { get; set; }

        public RaiseToolTipEventArgs(string text, int displayDuration)
        {
            this.Text = text;
            this.DisplayDuration = displayDuration;
        }
    }
}
