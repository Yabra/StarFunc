using System;
using System.Collections.Generic;

namespace StarFunc.Data
{
    public class PopupAction
    {
        public string Label;
        public Action Callback;
    }

    public class PopupData
    {
        public string Title;
        public string Message;
        public List<PopupAction> Actions;
    }
}
