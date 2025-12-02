using System;
using UnityEngine;

namespace Genies.UI.Widgets
{
    public class SimpleBreadcrumb : IBreadcrumb
    {
        public string Title { get; set; }
        public string BreadcrumbId => _breadcrumbId;

        private string _breadcrumbId;

        private Action _action;

        public SimpleBreadcrumb(string breadcrumbId, string title, Action action)
        {
            _breadcrumbId = breadcrumbId;
            Title = title ?? breadcrumbId;
            _action = action;

        }

        public void BreadcumbAction()
        {
            _action?.Invoke();
        }
    }
}
