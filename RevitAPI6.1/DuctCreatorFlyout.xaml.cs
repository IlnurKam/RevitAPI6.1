using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RevitAPI6._1
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DuctCreatorFlyout : ContentPage
    {
        public ListView ListView;

        public DuctCreatorFlyout()
        {
            InitializeComponent();

            BindingContext = new DuctCreatorFlyoutViewModel();
            ListView = MenuItemsListView;
        }

        class DuctCreatorFlyoutViewModel : INotifyPropertyChanged
        {
            public ObservableCollection<DuctCreatorFlyoutMenuItem> MenuItems { get; set; }
            
            public DuctCreatorFlyoutViewModel()
            {
                MenuItems = new ObservableCollection<DuctCreatorFlyoutMenuItem>(new[]
                {
                    new DuctCreatorFlyoutMenuItem { Id = 0, Title = "Page 1" },
                    new DuctCreatorFlyoutMenuItem { Id = 1, Title = "Page 2" },
                    new DuctCreatorFlyoutMenuItem { Id = 2, Title = "Page 3" },
                    new DuctCreatorFlyoutMenuItem { Id = 3, Title = "Page 4" },
                    new DuctCreatorFlyoutMenuItem { Id = 4, Title = "Page 5" },
                });
            }
            
            #region INotifyPropertyChanged Implementation
            public event PropertyChangedEventHandler PropertyChanged;
            void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                if (PropertyChanged == null)
                    return;

                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }
    }
}