using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AlalykinKP
{
    public partial class DeletedPage : Page
    {
        public DeletedPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var context = MilitaryDistrictEntities.GetContext();
            // Загружаем только УДАЛЕННЫЕ записи
            DeletedList.ItemsSource = context.Личный_состав
                .Include("Звание")
                .Where(p => p.is_deleted == true)
                .ToList();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as Личный_состав;
            if (item == null) return;

            if (MessageBox.Show("Восстановить эту запись?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var context = MilitaryDistrictEntities.GetContext();
                item.is_deleted = false; // Снимаем флаг удаления
                context.SaveChanges();
                LoadData(); // Обновляем список в корзине
            }
        }

    }
}