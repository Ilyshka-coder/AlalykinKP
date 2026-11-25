using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AlalykinKP
{
    public partial class PersonalPage : Page
    {
        public PersonalPage()
        {
            InitializeComponent();
            ComboType.SelectedIndex = 0;
            ComboZvanie.SelectedIndex = 0;
            ComboStatus.SelectedIndex = 0;
            SortType.SelectedIndex = 0;
            UpdateItems();
            this.Loaded += PersonalPage_Loaded;
        }

        private void PersonalPage_Loaded(object sender, RoutedEventArgs e) => UpdateItems();

        private void UpdateItems()
        {
            var context = MilitaryDistrictEntities.GetContext();

            // Загружаем только НЕудаленные
            var query = context.Личный_состав
                .Include("Роты")
                .Include("Звание")
                .Where(p => p.is_deleted == false)
                .ToList();

            // ФИЛЬТРАЦИЯ
            if (ComboType.SelectedIndex > 0)
            {
                int selectedRotaId = ComboType.SelectedIndex;
                query = query.Where(p => p.id_rota == selectedRotaId).ToList();
            }

            if (ComboZvanie.SelectedIndex > 0)
            {
                string selectedZvanie = (ComboZvanie.SelectedItem as TextBlock).Text;
                query = query.Where(p => p.Звание != null && p.Звание.zvanie == selectedZvanie).ToList();
            }

            bool hasNagrady(Личный_состав p) => !string.IsNullOrWhiteSpace(p.nagrady) && p.nagrady.Trim().ToLower() != "нет";
            bool hasUchastie(Личный_состав p) => !string.IsNullOrWhiteSpace(p.uchastie) && p.uchastie.Trim().ToLower() != "не участвовал";

            if (ComboStatus.SelectedIndex == 1)
                query = query.Where(p => hasNagrady(p) && !hasUchastie(p)).ToList();
            else if (ComboStatus.SelectedIndex == 2)
                query = query.Where(p => !hasNagrady(p) && hasUchastie(p)).ToList();
            else if (ComboStatus.SelectedIndex == 3)
                query = query.Where(p => hasNagrady(p) && hasUchastie(p)).ToList();
            else if (ComboStatus.SelectedIndex == 4)
                query = query.Where(p => !hasNagrady(p) && !hasUchastie(p)).ToList();

            // ПОИСК
            string searchText = TBoxSearch.Text.Replace(" ", string.Empty).ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(p =>
                {
                    string allText =
                        (p.familya ?? "") + (p.imya ?? "") + (p.otchestvo ?? "") +
                        (p.Звание != null ? p.Звание.zvanie : "") +
                        (p.Роты != null ? p.Роты.nazvanie_roty : "") +
                        (p.dolzhnost ?? "") +
                        p.data_rozhdeniya.ToString("dd.MM.yyyy") +
                        p.data_postupleniya.ToString("dd.MM.yyyy") +
                        (p.nagrady ?? "") + (p.uchastie ?? "");
                    return allText.Replace(" ", string.Empty).ToLower().Contains(searchText);
                }).ToList();
            }

            // СОРТИРОВКА
            switch (SortType.SelectedIndex)
            {
                case 1: query = query.OrderBy(p => p.familya).ToList(); break;
                case 2: query = query.OrderByDescending(p => p.familya).ToList(); break;
                case 3: query = query.OrderByDescending(p => p.data_rozhdeniya).ToList(); break;
                case 4: query = query.OrderBy(p => p.data_rozhdeniya).ToList(); break;
                case 5: query = query.OrderByDescending(p => p.data_postupleniya).ToList(); break;
                case 6: query = query.OrderBy(p => p.data_postupleniya).ToList(); break;
                case 7: query = query.OrderBy(p => p.zvanie).ToList(); break;
                case 8: query = query.OrderByDescending(p => p.zvanie).ToList(); break;
            }

            ListView.ItemsSource = query;
            personalCount.Text = "Объектов на странице: " + query.Count;
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateItems();
        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateItems();
        private void ComboZvanie_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateItems();
        private void ComboStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateItems();
        private void SortType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateItems();

        private void AddButton_Click(object sender, RoutedEventArgs e) => Manager.MainFrame.Navigate(new AddEditPage(null));
        private void EditButton_Click(object sender, RoutedEventArgs e) => Manager.MainFrame.Navigate(new AddEditPage((sender as Button).DataContext as Личный_состав));
        private void DeletedItemsButton_Click(object sender, RoutedEventArgs e) => Manager.MainFrame.Navigate(new DeletedPage());

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var currentItem = (sender as Button).DataContext as Личный_состав;
            if (currentItem == null) return;

            var historyCount = MilitaryDistrictEntities.GetContext().История_званий
                .Count(h => h.id_soldat == currentItem.id_soldat);

            if (historyCount == 0)
            {
                MessageBox.Show($"У военнослужащего {currentItem.familya} нет записей об изменении звания.",
                                "История пуста", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                HistoryWindow historyWin = new HistoryWindow(currentItem);
                historyWin.Owner = Window.GetWindow(this);
                historyWin.ShowDialog();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var currentItem = (sender as Button).DataContext as Личный_состав;
            if (currentItem == null) return;
            if (currentItem.zvanie==20)
            {
                MessageBox.Show("Нельзя удалить Маршала Российской Федерации!\nПоменяйте звание","Внимание!",MessageBoxButton.OK);
            }
            else
            {
                if (MessageBox.Show("Вы точно хотите выполнить удаление?",
                    "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var context = MilitaryDistrictEntities.GetContext();
                        
                        currentItem.is_deleted = true; // Отправляем в корзину
                        context.SaveChanges();
                        UpdateItems(); // Обновляем список на экране
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                    }
                }
            }
        }
    }
}