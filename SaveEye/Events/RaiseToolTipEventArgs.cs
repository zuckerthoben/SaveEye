using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveEye.Events
{
    public class RaiseToolTipEventArgs : EventArgs
    {
        public string _Text { get; set; }
        public int _DisplayDuration { get; set; }

        public RaiseToolTipEventArgs(string text, int displayduration)
        {
            this._Text = text;
            this._DisplayDuration = displayduration;
        }
    }
}
