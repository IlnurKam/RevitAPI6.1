using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI6._1
{

    public class DuctCreatorFlyoutMenuItem
    {
        public DuctCreatorFlyoutMenuItem()
        {
            TargetType = typeof(DuctCreatorFlyoutMenuItem);
        }
        public int Id { get; set; }
        public string Title { get; set; }

        public Type TargetType { get; set; }
    }
}