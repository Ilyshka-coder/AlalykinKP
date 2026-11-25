using Microsoft.Win32;
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Не забудьте добавить using для Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AlalykinKP
{
    public partial class AddEditPage : Page
    {
        private Личный_состав _currentItem = new Личный_состав();
        private int? _oldZvanieId = null;

        public AddEditPage(Личный_состав SelectedItem)
        {
            InitializeComponent();

            ZvanieCombo.ItemsSource = MilitaryDistrictEntities.GetContext().Звание.ToList();
            ZvanieCombo.DisplayMemberPath = "zvanie";
            ZvanieCombo.SelectedValuePath = "id_zvanie";

            RotaCombo.Items.Add("мотострелковая рота");
            RotaCombo.Items.Add("танковая рота");
            RotaCombo.Items.Add("воздушно-десантная рота");
            RotaCombo.Items.Add("рота морской пехоты");
            RotaCombo.Items.Add("рота БПЛА");

            if (SelectedItem != null)
            {
                _currentItem = SelectedItem;

                RotaCombo.SelectedIndex = _currentItem.id_rota.Value - 1;
                BirhDateDP.SelectedDate = _currentItem.data_rozhdeniya;
                SurnameTB.Text = _currentItem.familya;
                NameTB.Text = _currentItem.imya;
                PatronymicTB.Text = _currentItem.otchestvo;
                DolzhnostTB.Text = _currentItem.dolzhnost;
                NagradyTB.Text = _currentItem.nagrady;
                UchastieTB.Text = _currentItem.uchastie;
                PostuplenieDP.SelectedDate = _currentItem.data_postupleniya;

                if (_currentItem.zvanie != null)
                {
                    ZvanieCombo.SelectedValue = _currentItem.zvanie;
                    _oldZvanieId = _currentItem.zvanie;
                }

                if (!string.IsNullOrEmpty(_currentItem.image))
                {
                    try { BigImage.Source = new BitmapImage(new Uri(_currentItem.ImageFull, UriKind.RelativeOrAbsolute)); }
                    catch { }
                }
            }
            else
            {
                // НАСТРОЙКИ ДЛЯ НОВОЙ ЗАПИСИ
                _currentItem.is_deleted = false; // Новая запись не удалена
                PostuplenieDP.SelectedDate = DateTime.Today;
                ZvanieCombo.SelectedIndex = 0;

                // УСТАНОВКА ЗАГЛУШКИ ДЛЯ ФОТО
                string defaultImagePath = "/Images/not_found.png";
                _currentItem.image = defaultImagePath;

                // Проверяем, существует ли файл заглушки в папке с программой, чтобы не было ошибки
                try
                {
                    BigImage.Source = new BitmapImage(new Uri(defaultImagePath, UriKind.Relative));
                }
                catch
                {
                    // Если файла нет, картинка просто не отобразится, но программа не упадет
                }
            }

            DataContext = _currentItem;
        }

        // СТРОГИЙ МЕТОД ОЧИСТКИ ФИО
        private string CleanName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Оставляем только русские и английские буквы (удаляем цифры, пробелы, дефисы и спецсимволы)
            string onlyLetters = Regex.Replace(input, @"[^a-zA-Zа-яА-ЯёЁ]", "");

            if (string.IsNullOrEmpty(onlyLetters)) return "";

            // Делаем первую букву заглавной, остальные маленькими
            return char.ToUpper(onlyLetters[0]) + onlyLetters.Substring(1).ToLower();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            // 1. Проверка ФИО
            if (string.IsNullOrWhiteSpace(SurnameTB.Text)) errors.AppendLine("Введите фамилию");
            if (string.IsNullOrWhiteSpace(NameTB.Text)) errors.AppendLine("Введите имя");
            if (string.IsNullOrWhiteSpace(PatronymicTB.Text)) errors.AppendLine("Введите отчество");

            // 2. Проверка Даты Рождения (18-100 лет)
            if (string.IsNullOrWhiteSpace(BirhDateDP.Text))
            {
                errors.AppendLine("Введите дату рождения");
            }
            else
            {
                DateTime birthDate = Convert.ToDateTime(BirhDateDP.Text);
                if (birthDate > DateTime.Today.AddYears(-18))
                    errors.AppendLine("Возраст должен быть не менее 18 лет");
                if (birthDate < DateTime.Today.AddYears(-100))
                    errors.AppendLine("Возраст должен быть не более 100 лет");
            }

            // 3. Проверка Должности
            if (string.IsNullOrWhiteSpace(DolzhnostTB.Text)) errors.AppendLine("Введите должность");

            // 4. Проверка Роты
            if (RotaCombo.SelectedIndex == -1) errors.AppendLine("Выберите роту из списка");

            // 5. Проверка Звания
            if (ZvanieCombo.SelectedValue == null) errors.AppendLine("Выберите звание из списка");

            // 6. Проверка Даты Поступления
            if (string.IsNullOrWhiteSpace(PostuplenieDP.Text)) errors.AppendLine("Выберите дату поступления");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            // Заполнение данных с использованием строгого метода очистки
            _currentItem.familya = CleanName(SurnameTB.Text);
            _currentItem.imya = CleanName(NameTB.Text);
            _currentItem.otchestvo = CleanName(PatronymicTB.Text);

            // Если после очистки поле стало пустым (например, ввели только цифры), выдаем ошибку
            if (string.IsNullOrEmpty(_currentItem.familya)) { MessageBox.Show("Фамилия должна содержать буквы!"); return; }
            if (string.IsNullOrEmpty(_currentItem.imya)) { MessageBox.Show("Имя должно содержать буквы!"); return; }
            if (string.IsNullOrEmpty(_currentItem.otchestvo)) { MessageBox.Show("Отчество должно содержать буквы!"); return; }

            _currentItem.data_rozhdeniya = Convert.ToDateTime(BirhDateDP.Text);
            _currentItem.data_postupleniya = Convert.ToDateTime(PostuplenieDP.Text);

            _currentItem.dolzhnost = DolzhnostTB.Text.Trim();
            _currentItem.nagrady = string.IsNullOrWhiteSpace(NagradyTB.Text) ? "нет" : NagradyTB.Text.Trim();
            _currentItem.uchastie = string.IsNullOrWhiteSpace(UchastieTB.Text) ? "не участвовал" : UchastieTB.Text.Trim();

            _currentItem.id_rota = RotaCombo.SelectedIndex + 1;

            int selectedZvanieId = Convert.ToInt32(ZvanieCombo.SelectedValue);
            _currentItem.zvanie = selectedZvanieId;

            if (BigImage.Source != null) _currentItem.image = BigImage.Source.ToString();

            var context = MilitaryDistrictEntities.GetContext();

            try
            {
                if (_currentItem.id_soldat == 0)
                {
                    context.Личный_состав.Add(_currentItem);
                }
                else
                {
                    // Если звание изменилось, пишем СТАРОЕ звание в историю
                    if (_oldZvanieId != selectedZvanieId && _oldZvanieId != null)
                    {
                        context.История_званий.Add(new История_званий
                        {
                            id_soldat = _currentItem.id_soldat,
                            id_zvanie = _oldZvanieId.Value,
                            date_change = DateTime.Now
                        });
                    }
                }

                context.SaveChanges();
                MessageBox.Show("Информация сохранена!");
                Manager.MainFrame.GoBack();
            }
            catch (DbUpdateException ex)
            {
                Exception inner = ex;
                string fullError = "";
                while (inner != null)
                {
                    fullError += inner.Message + "\n\n---\n\n";
                    inner = inner.InnerException;
                }
                MessageBox.Show(fullError, "Причина ошибки");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    BigImage.Source = new BitmapImage(new Uri(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки изображения: " + ex.Message);
                }
            }
        }
    }
}