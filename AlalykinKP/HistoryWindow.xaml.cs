using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AlalykinKP
{
    public partial class HistoryWindow : Window
    {
        private Личный_состав solderID;
        public HistoryWindow(Личный_состав soldier)
        {
            InitializeComponent();
            solderID = soldier;

            TitleText.Text = $"История званий: {soldier.familya} {soldier.imya} {soldier.otchestvo}";

            // Подгружаем связанную таблицу Звание
            var history = MilitaryDistrictEntities.GetContext().История_званий
                .Include("Звание")
                .Where(h => h.id_soldat == soldier.id_soldat)
                .OrderByDescending(h => h.date_change)
                .ToList();

            HistoryList.ItemsSource = history;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var currentZvanie = (sender as Button).DataContext as История_званий;
            try
            {
                MilitaryDistrictEntities.GetContext().История_званий.Remove(currentZvanie);
                MilitaryDistrictEntities.GetContext().SaveChanges();
                var history = MilitaryDistrictEntities.GetContext().История_званий
                .Include("Звание")
                .Where(h => h.id_soldat == solderID.id_soldat)
                .OrderByDescending(h => h.date_change)
                .ToList();
                if (history.Count > 0) HistoryList.ItemsSource = history;
                else this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
    }
}