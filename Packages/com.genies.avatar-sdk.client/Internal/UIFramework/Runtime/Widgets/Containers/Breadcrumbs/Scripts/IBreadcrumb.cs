using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genies.UI.Widgets
{
    public interface IBreadcrumb
    {
        string BreadcrumbId { get; }
        string Title { get; set; }

        void BreadcumbAction();
    }
}
