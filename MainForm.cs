using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace VeloShop
{
    public class MainForm : Form
    {
        // Панели разделов
        private Panel pnlHeader = null!;
        private Panel pnlNav = null!;
        private Panel pnlContent = null!;

        // Кнопки навигации
        private Button btnNavCatalog = null!;
        private Button btnNavCart = null!;
        private Button btnNavFavorites = null!;
        private Button btnNavOrders = null!;
        private Button btnNavProfile = null!;
        private Button btnNavAdmin = null!;
        private Button btnNavAuth = null!;

        private Label lblUserStatus = null!;

        public MainForm()
        {
            InitializeComponent();
            ShowCatalogTab();
            UpdateUserNav();
        }

        private void InitializeComponent()
        {
            this.Text = "VeloShop — Магазин Велосипедов и Аксессуаров";
            this.Size = new Size(1150, 750);
            this.MinimumSize = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // 1. Верхний Хедер (Header)
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(30, 35, 45)
            };

            var lblLogo = new Label
            {
                Text = "🚴 VELOSHOP",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 195, 255),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            lblUserStatus = new Label
            {
                Text = "Гость",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(850, 22)
            };

            btnNavAuth = CreateHeaderButton("Войти / Регистрация", 980);
            btnNavAuth.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNavAuth.Click += (s, e) => OpenAuthDialog();

            pnlHeader.Controls.Add(lblLogo);
            pnlHeader.Controls.Add(lblUserStatus);
            pnlHeader.Controls.Add(btnNavAuth);

            // 2. Навигационная Панель (Sub-header)
            pnlNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(15, 5, 15, 5)
            };

            btnNavCatalog = CreateNavButton("🚲 Каталог", 15);
            btnNavCatalog.Click += (s, e) => ShowCatalogTab();

            btnNavFavorites = CreateNavButton("❤️ Избранное", 145);
            btnNavFavorites.Click += (s, e) => ShowFavoritesTab();

            btnNavCart = CreateNavButton("🛒 Корзина", 275);
            btnNavCart.Click += (s, e) => ShowCartTab();

            btnNavOrders = CreateNavButton("📦 Мои Заказы", 405);
            btnNavOrders.Click += (s, e) => ShowOrdersTab();

            btnNavProfile = CreateNavButton("👤 Профиль", 535);
            btnNavProfile.Click += (s, e) => ShowProfileTab();

            btnNavAdmin = CreateNavButton("🛠️ Панель Админа", 665);
            btnNavAdmin.BackColor = Color.FromArgb(255, 235, 235);
            btnNavAdmin.ForeColor = Color.DarkRed;
            btnNavAdmin.Visible = false;
            btnNavAdmin.Click += (s, e) => ShowAdminTab();

            pnlNav.Controls.Add(btnNavCatalog);
            pnlNav.Controls.Add(btnNavFavorites);
            pnlNav.Controls.Add(btnNavCart);
            pnlNav.Controls.Add(btnNavOrders);
            pnlNav.Controls.Add(btnNavProfile);
            pnlNav.Controls.Add(btnNavAdmin);

            // 3. Основная контентная панель
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(20)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlNav);
            this.Controls.Add(pnlHeader);
        }

        private Button CreateHeaderButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 150, 240),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 34),
                Location = new Point(x, 15),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateNavButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 60, 75),
                BackColor = Color.FromArgb(240, 243, 246),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 38),
                Location = new Point(x, 6),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public void UpdateUserNav()
        {
            if (Database.CurrentUser == null)
            {
                lblUserStatus.Text = "Режим: Гость";
                btnNavAuth.Text = "Войти / Регистрация";
                btnNavAdmin.Visible = false;
            }
            else
            {
                lblUserStatus.Text = $"Привет, {Database.CurrentUser.Login} ({Database.CurrentUser.Role})";
                btnNavAuth.Text = "Выйти";
                btnNavAdmin.Visible = Database.CurrentUser.Role == "Admin";
            }
        }

        private void OpenAuthDialog()
        {
            if (Database.CurrentUser != null)
            {
                if (MessageBox.Show("Вы действительно хотите выйти из своего аккаунта?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Database.CurrentUser = null;
                    UpdateUserNav();
                    ShowCatalogTab();
                }
                return;
            }

            using var authForm = new AuthForm();
            if (authForm.ShowDialog() == DialogResult.OK)
            {
                UpdateUserNav();
                ShowCatalogTab();
            }
        }

        // --- Вкладка 1: КАТАЛОГ ---
        private void ShowCatalogTab(bool onlyFavorites = false)
        {
            pnlContent.Controls.Clear();

            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            var txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Поиск велосипеда по названию...",
                Font = new Font("Segoe UI", 10),
                Size = new Size(250, 30),
                Location = new Point(15, 12)
            };

            var cmbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Size = new Size(150, 30),
                Location = new Point(280, 12)
            };
            cmbCategory.Items.AddRange(new string[] { "Все", "Горные", "Шоссейные", "Трековые", "Городские", "Аксессуары" });
            cmbCategory.SelectedIndex = 0;

            var lblPrice = new Label
            {
                Text = "Цена до:",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(445, 15),
                AutoSize = true
            };

            var txtMaxPrice = new TextBox
            {
                PlaceholderText = "100000",
                Font = new Font("Segoe UI", 10),
                Size = new Size(90, 30),
                Location = new Point(510, 12)
            };

            var btnApply = new Button
            {
                Text = "Применить",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 150, 240),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 30),
                Location = new Point(615, 12),
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;

            pnlFilter.Controls.Add(txtSearch);
            pnlFilter.Controls.Add(cmbCategory);
            pnlFilter.Controls.Add(lblPrice);
            pnlFilter.Controls.Add(txtMaxPrice);
            pnlFilter.Controls.Add(btnApply);

            var flowList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Локальный метод вместо Action - решает ошибку CS0165 раз и навсегда!
            void LoadProducts()
            {
                flowList.Controls.Clear();
                double maxP = double.TryParse(txtMaxPrice.Text, out double p) ? p : double.MaxValue;
                int uid = Database.CurrentUser?.Id ?? 0;

                var products = Database.GetProducts(txtSearch.Text, cmbCategory.SelectedItem?.ToString() ?? "Все", 0, maxP, onlyFavorites, uid);

                if (products.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = onlyFavorites ? "У вас пока нет избранных товаров." : "Товары по вашему запросу не найдены.",
                        Font = new Font("Segoe UI", 12, FontStyle.Italic),
                        ForeColor = Color.Gray,
                        AutoSize = true,
                        Margin = new Padding(20)
                    };
                    flowList.Controls.Add(lblEmpty);
                    return;
                }

                foreach (var prod in products)
                {
                    flowList.Controls.Add(CreateProductCard(prod, LoadProducts));
                }
            }

            btnApply.Click += (s, e) => LoadProducts();
            txtSearch.TextChanged += (s, e) => LoadProducts();

            pnlContent.Controls.Add(flowList);
            pnlContent.Controls.Add(pnlFilter);

            LoadProducts();
        }

        private Panel CreateProductCard(Product p, Action refreshCatalog)
        {
            var card = new Panel
            {
                Size = new Size(250, 340),
                Margin = new Padding(12),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var pic = new PictureBox
            {
                Size = new Size(230, 140),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            // Генерация картинки-заглушки векторным способом, если нет локального пути
            if (!string.IsNullOrEmpty(p.ImagePath) && File.Exists(p.ImagePath))
            {
                try { pic.Image = Image.FromFile(p.ImagePath); } catch { pic.Image = DrawBikeStub(); }
            }
            else
            {
                pic.Image = DrawBikeStub();
            }

            var lblCategory = new Label
            {
                Text = p.Category.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 240),
                Location = new Point(10, 155),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = p.Name,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 35, 45),
                Location = new Point(10, 172),
                Size = new Size(230, 40)
            };

            var lblPrice = new Label
            {
                Text = $"{p.Price:N0} ₽",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 160, 70),
                Location = new Point(10, 215),
                AutoSize = true
            };

            var lblStock = new Label
            {
                Text = p.Stock > 0 ? $"В наличии: {p.Stock} шт." : "Нет в наличии",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = p.Stock > 0 ? Color.Gray : Color.Red,
                Location = new Point(10, 242),
                AutoSize = true
            };

            var btnBuy = new Button
            {
                Text = p.Stock > 0 ? "В корзину 🛒" : "Раскуплено",
                Enabled = p.Stock > 0,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = p.Stock > 0 ? Color.FromArgb(30, 35, 45) : Color.LightGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 34),
                Location = new Point(10, 285),
                Cursor = p.Stock > 0 ? Cursors.Hand : Cursors.Default
            };
            btnBuy.FlatAppearance.BorderSize = 0;

            int uid = Database.CurrentUser?.Id ?? 0;
            bool isFav = Database.IsFavorite(uid, p.Id);

            var btnFav = new Button
            {
                Text = isFav ? "❤️" : "🤍",
                Font = new Font("Segoe UI", 12),
                Size = new Size(50, 34),
                Location = new Point(180, 285),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 247, 250),
                Cursor = Cursors.Hand
            };
            btnFav.FlatAppearance.BorderSize = 0;

            btnBuy.Click += (s, e) =>
            {
                if (Database.CurrentUser == null)
                {
                    MessageBox.Show("Для добавления товара в корзину войдите в систему!", "Авторизация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    OpenAuthDialog();
                    return;
                }
                Database.AddToCart(Database.CurrentUser.Id, p.Id, 1);
                MessageBox.Show($"Товар '{p.Name}' добавлен в корзину!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnFav.Click += (s, e) =>
            {
                if (Database.CurrentUser == null)
                {
                    MessageBox.Show("Для добавления в избранное войдите в систему!", "Авторизация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    OpenAuthDialog();
                    return;
                }
                bool nowFav = Database.ToggleFavorite(Database.CurrentUser.Id, p.Id);
                btnFav.Text = nowFav ? "❤️" : "🤍";
                refreshCatalog();
            };

            card.Controls.Add(pic);
            card.Controls.Add(lblCategory);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblPrice);
            card.Controls.Add(lblStock);
            card.Controls.Add(btnBuy);
            card.Controls.Add(btnFav);

            return card;
        }

        private Bitmap DrawBikeStub()
        {
            var bmp = new Bitmap(230, 140);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(240, 242, 245));

            using var pen = new Pen(Color.FromArgb(120, 130, 150), 3);
            // Колеса
            g.DrawEllipse(pen, 30, 60, 50, 50);
            g.DrawEllipse(pen, 150, 60, 50, 50);
            // Рама
            g.DrawLine(pen, 55, 85, 95, 85);
            g.DrawLine(pen, 95, 85, 130, 50);
            g.DrawLine(pen, 130, 50, 75, 50);
            g.DrawLine(pen, 75, 50, 55, 85);
            g.DrawLine(pen, 130, 50, 175, 85);
            // Руль и седло
            g.DrawLine(pen, 125, 45, 135, 45);
            g.DrawLine(pen, 70, 45, 80, 45);

            return bmp;
        }

        private void ShowFavoritesTab()
        {
            if (Database.CurrentUser == null)
            {
                MessageBox.Show("Раздел 'Избранное' доступен только зарегистрированным пользователям!", "Авторизация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenAuthDialog();
                return;
            }
            ShowCatalogTab(onlyFavorites: true);
        }

        // --- Вкладка 2: КОРЗИНА ---
        private void ShowCartTab()
        {
            if (Database.CurrentUser == null)
            {
                MessageBox.Show("Корзина доступна только авторизованным пользователям!", "Вход", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenAuthDialog();
                return;
            }

            pnlContent.Controls.Clear();

            var cartItems = Database.GetCart(Database.CurrentUser.Id);

            var lblTitle = new Label
            {
                Text = "🛒 Ваша Корзина",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            var grid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(700, 480),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "ID");
            grid.Columns.Add("Name", "Наименование");
            grid.Columns.Add("Price", "Цена (₽)");
            grid.Columns.Add("Qty", "Количество");
            grid.Columns.Add("Total", "Сумма (₽)");

            double grandTotal = 0;
            foreach (var item in cartItems)
            {
                double sum = item.Price * item.Quantity;
                grandTotal += sum;
                grid.Rows.Add(item.Id, item.ProductName, item.Price.ToString("N0"), item.Quantity, sum.ToString("N0"));
            }

            // Панель оформления
            var pnlCheckout = new Panel
            {
                Location = new Point(730, 50),
                Size = new Size(350, 480),
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var lblSummary = new Label
            {
                Text = $"Итого к оплате:\n{grandTotal:N0} ₽",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 160, 70),
                Location = new Point(15, 15),
                Size = new Size(320, 60)
            };

            var lblAddr = new Label { Text = "Адрес доставки:", Location = new Point(15, 90), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            var txtAddr = new TextBox { Text = Database.CurrentUser.Address, Location = new Point(15, 115), Size = new Size(320, 30), Font = new Font("Segoe UI", 10) };

            var lblPhone = new Label { Text = "Контактный телефон:", Location = new Point(15, 155), AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
            var txtPhone = new TextBox { Text = Database.CurrentUser.Phone, Location = new Point(15, 180), Size = new Size(320, 30), Font = new Font("Segoe UI", 10) };

            var btnCheckout = new Button
            {
                Text = "Оформить Заказ 🚀",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 160, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(320, 45),
                Location = new Point(15, 240),
                Cursor = Cursors.Hand
            };
            btnCheckout.FlatAppearance.BorderSize = 0;

            var btnClear = new Button
            {
                Text = "Очистить корзину 🗑️",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(245, 247, 250),
                Size = new Size(320, 35),
                Location = new Point(15, 300),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnClear.Click += (s, e) =>
            {
                Database.ClearCart(Database.CurrentUser.Id);
                ShowCartTab();
            };

            btnCheckout.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtAddr.Text) || string.IsNullOrWhiteSpace(txtPhone.Text))
                {
                    MessageBox.Show("Укажите адрес и телефон для доставки!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var res = Database.Checkout(Database.CurrentUser.Id, txtAddr.Text, txtPhone.Text);
                MessageBox.Show(res.Message, res.Success ? "Успешно" : "Ошибка", MessageBoxButtons.OK, res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                if (res.Success)
                {
                    ShowOrdersTab();
                }
            };

            pnlCheckout.Controls.Add(lblSummary);
            pnlCheckout.Controls.Add(lblAddr);
            pnlCheckout.Controls.Add(txtAddr);
            pnlCheckout.Controls.Add(lblPhone);
            pnlCheckout.Controls.Add(txtPhone);
            pnlCheckout.Controls.Add(btnCheckout);
            pnlCheckout.Controls.Add(btnClear);

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(grid);
            pnlContent.Controls.Add(pnlCheckout);
        }

        // --- Вкладка 3: ИСТОРИЯ ЗАКАЗОВ ---
        private void ShowOrdersTab()
        {
            if (Database.CurrentUser == null)
            {
                MessageBox.Show("История заказов доступна только авторизованным пользователям!", "Вход", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenAuthDialog();
                return;
            }

            pnlContent.Controls.Clear();

            var lblTitle = new Label
            {
                Text = "📦 История ваших заказов",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            var orders = Database.GetOrders(Database.CurrentUser.Id);

            var tree = new TreeView
            {
                Location = new Point(10, 50),
                Size = new Size(1070, 520),
                Font = new Font("Segoe UI", 10)
            };

            foreach (var ord in orders)
            {
                var parentNode = new TreeNode($"Заказ №{ord.Id} от {ord.OrderDate} — Итого: {ord.TotalPrice:N0} ₽ [{ord.Status}]");
                parentNode.Nodes.Add($"Адрес доставки: {ord.DeliveryAddress}");
                parentNode.Nodes.Add($"Телефон: {ord.ContactPhone}");

                var itemsNode = new TreeNode("Состав заказа:");
                foreach (var item in ord.Items)
                {
                    itemsNode.Nodes.Add($"{item.ProductName} — {item.Quantity} шт. x {item.PriceAtPurchase:N0} ₽ = {item.Quantity * item.PriceAtPurchase:N0} ₽");
                }
                parentNode.Nodes.Add(itemsNode);
                tree.Nodes.Add(parentNode);
            }

            tree.ExpandAll();

            pnlContent.Controls.Add(lblTitle);
            pnlContent.Controls.Add(tree);
        }

        // --- Вкладка 4: ПРОФИЛЬ ---
        private void ShowProfileTab()
        {
            if (Database.CurrentUser == null)
            {
                OpenAuthDialog();
                return;
            }

            pnlContent.Controls.Clear();

            var pnl = new Panel
            {
                Size = new Size(500, 560),
                Location = new Point(280, 20),
                BackColor = Color.White,
                Padding = new Padding(25)
            };

            var lblTitle = new Label { Text = "👤 Личный Профиль", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true };

            var lblLogin = new Label { Text = $"Логин: {Database.CurrentUser.Login} ({Database.CurrentUser.Role})", Font = new Font("Segoe UI", 10, FontStyle.Italic), Location = new Point(25, 60), AutoSize = true };

            var lblName = new Label { Text = "ФИО:", Location = new Point(25, 100), AutoSize = true };
            var txtName = new TextBox { Text = Database.CurrentUser.FullName, Location = new Point(25, 125), Size = new Size(440, 30), Font = new Font("Segoe UI", 10) };

            var lblPhone = new Label { Text = "Номер телефона:", Location = new Point(25, 165), AutoSize = true };
            var txtPhone = new TextBox { Text = Database.CurrentUser.Phone, Location = new Point(25, 190), Size = new Size(440, 30), Font = new Font("Segoe UI", 10) };

            var lblAddr = new Label { Text = "Адрес доставки:", Location = new Point(25, 230), AutoSize = true };
            var txtAddr = new TextBox { Text = Database.CurrentUser.Address, Location = new Point(25, 255), Size = new Size(440, 30), Font = new Font("Segoe UI", 10) };

            var btnSave = new Button
            {
                Text = "Сохранить изменения",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 160, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(440, 40),
                Location = new Point(25, 305),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;

            btnSave.Click += (s, e) =>
            {
                Database.UpdateProfile(Database.CurrentUser.Id, txtName.Text, txtPhone.Text, txtAddr.Text, Database.CurrentUser.AvatarPath);
                Database.CurrentUser.FullName = txtName.Text;
                Database.CurrentUser.Phone = txtPhone.Text;
                Database.CurrentUser.Address = txtAddr.Text;
                MessageBox.Show("Данные профиля успешно обновлены!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Смена пароля
            var lblPassTitle = new Label { Text = "Смена пароля:", Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(25, 365), AutoSize = true };
            var txtOldPass = new TextBox { PlaceholderText = "Старый пароль", PasswordChar = '*', Location = new Point(25, 395), Size = new Size(215, 30) };
            var txtNewPass = new TextBox { PlaceholderText = "Новый пароль", PasswordChar = '*', Location = new Point(250, 395), Size = new Size(215, 30) };

            var btnChangePass = new Button
            {
                Text = "Обновить пароль",
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(30, 35, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(440, 35),
                Location = new Point(25, 440),
                Cursor = Cursors.Hand
            };

            btnChangePass.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtOldPass.Text) || string.IsNullOrWhiteSpace(txtNewPass.Text))
                {
                    MessageBox.Show("Заполните оба поля для смены пароля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (Database.ChangePassword(Database.CurrentUser.Id, txtOldPass.Text, txtNewPass.Text))
                {
                    MessageBox.Show("Пароль успешно изменен!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtOldPass.Clear();
                    txtNewPass.Clear();
                }
                else
                {
                    MessageBox.Show("Старый пароль введен неверно!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblLogin);
            pnl.Controls.Add(lblName);
            pnl.Controls.Add(txtName);
            pnl.Controls.Add(lblPhone);
            pnl.Controls.Add(txtPhone);
            pnl.Controls.Add(lblAddr);
            pnl.Controls.Add(txtAddr);
            pnl.Controls.Add(btnSave);
            pnl.Controls.Add(lblPassTitle);
            pnl.Controls.Add(txtOldPass);
            pnl.Controls.Add(txtNewPass);
            pnl.Controls.Add(btnChangePass);

            pnlContent.Controls.Add(pnl);
        }

        // --- Вкладка 5: ПАНЕЛЬ АДМИНИСТРАТОРА ---
        private void ShowAdminTab()
        {
            if (Database.CurrentUser == null || Database.CurrentUser.Role != "Admin")
            {
                MessageBox.Show("Доступ запрещен!", "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            pnlContent.Controls.Clear();

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            // Страница 1: Управление Товарами
            var pageProducts = new TabPage("📦 Управление Товарами");
            var gridProds = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 380,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            gridProds.Columns.Add("Id", "ID");
            gridProds.Columns.Add("Name", "Название");
            gridProds.Columns.Add("Category", "Категория");
            gridProds.Columns.Add("Price", "Цена (₽)");
            gridProds.Columns.Add("Stock", "Склад");

            void LoadProds()
            {
                gridProds.Rows.Clear();
                foreach (var p in Database.GetProducts())
                {
                    gridProds.Rows.Add(p.Id, p.Name, p.Category, p.Price, p.Stock);
                }
            }
            LoadProds();

            var pnlProdEdit = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var txtPName = new TextBox { PlaceholderText = "Название товара", Location = new Point(10, 15), Size = new Size(200, 30) };
            var cmbPCat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(220, 15), Size = new Size(130, 30) };
            cmbPCat.Items.AddRange(new string[] { "Горные", "Шоссейные", "Трековые", "Городские", "Аксессуары" });
            cmbPCat.SelectedIndex = 0;

            var txtPPrice = new TextBox { PlaceholderText = "Цена", Location = new Point(360, 15), Size = new Size(100, 30) };
            var txtPStock = new TextBox { PlaceholderText = "Остаток", Location = new Point(470, 15), Size = new Size(90, 30) };
            var txtPDesc = new TextBox { PlaceholderText = "Описание товара", Location = new Point(10, 55), Size = new Size(550, 30) };

            var btnAddProd = new Button { Text = "Сохранить/Добавить Товар", Location = new Point(570, 15), Size = new Size(200, 70), BackColor = Color.FromArgb(40, 160, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnAddProd.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPName.Text) || !double.TryParse(txtPPrice.Text, out double pr) || !int.TryParse(txtPStock.Text, out int st))
                {
                    MessageBox.Show("Заполните имя, корректную цену и остаток!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Database.SaveProduct(new Product
                {
                    Name = txtPName.Text,
                    Category = cmbPCat.SelectedItem?.ToString() ?? "Горные",
                    Price = pr,
                    Stock = st,
                    Description = txtPDesc.Text
                });
                LoadProds();
                MessageBox.Show("Товар успешно сохранен!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnlProdEdit.Controls.Add(txtPName);
            pnlProdEdit.Controls.Add(cmbPCat);
            pnlProdEdit.Controls.Add(txtPPrice);
            pnlProdEdit.Controls.Add(txtPStock);
            pnlProdEdit.Controls.Add(txtPDesc);
            pnlProdEdit.Controls.Add(btnAddProd);

            pageProducts.Controls.Add(pnlProdEdit);
            pageProducts.Controls.Add(gridProds);

            // Страница 2: Управление Заказами клиентов
            var pageOrders = new TabPage("🚚 Заказы Покупателей");
            var gridOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            gridOrders.Columns.Add("Id", "№ Заказа");
            gridOrders.Columns.Add("User", "Покупатель");
            gridOrders.Columns.Add("Date", "Дата");
            gridOrders.Columns.Add("Total", "Сумма (₽)");
            gridOrders.Columns.Add("Status", "Статус");

            void LoadAllOrders()
            {
                gridOrders.Rows.Clear();
                foreach (var o in Database.GetOrders())
                {
                    gridOrders.Rows.Add(o.Id, o.UserLogin, o.OrderDate, o.TotalPrice.ToString("N0"), o.Status);
                }
            }
            LoadAllOrders();

            pageOrders.Controls.Add(gridOrders);

            // Страница 3: Пользователи и Бан
            var pageUsers = new TabPage("👥 Пользователи и Бан");
            var gridUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            gridUsers.Columns.Add("Id", "ID");
            gridUsers.Columns.Add("Login", "Логин");
            gridUsers.Columns.Add("Name", "ФИО");
            gridUsers.Columns.Add("Role", "Роль");
            gridUsers.Columns.Add("Banned", "Заблокирован");

            void LoadUsers()
            {
                gridUsers.Rows.Clear();
                foreach (var u in Database.GetAllUsers())
                {
                    gridUsers.Rows.Add(u.Id, u.Login, u.FullName, u.Role, u.IsBanned ? "ДА 🚫" : "НЕТ");
                }
            }
            LoadUsers();

            var btnBan = new Button { Text = "Заблокировать / Разблокировать Выбранного", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.DarkRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnBan.Click += (s, e) =>
            {
                if (gridUsers.SelectedRows.Count > 0)
                {
                    var cellVal = gridUsers.SelectedRows[0].Cells[0].Value;
                    if (cellVal != null)
                    {
                        int uid = Convert.ToInt32(cellVal);
                        string status = gridUsers.SelectedRows[0].Cells[4].Value?.ToString() ?? "";
                        bool isBanned = status.Contains("ДА");
                        Database.ToggleBanUser(uid, !isBanned);
                        LoadUsers();
                    }
                }
            };

            pageUsers.Controls.Add(gridUsers);
            pageUsers.Controls.Add(btnBan);

            tabControl.TabPages.Add(pageProducts);
            tabControl.TabPages.Add(pageOrders);
            tabControl.TabPages.Add(pageUsers);

            pnlContent.Controls.Add(tabControl);
        }
    }

    // --- ФОРМА АВТОРИЗАЦИИ И РЕГИСТРАЦИИ ---
    public class AuthForm : Form
    {
        private TabControl tabs = null!;
        private TextBox txtLogLogin = null!, txtLogPass = null!;
        private TextBox txtRegLogin = null!, txtRegPass = null!, txtRegName = null!, txtRegPhone = null!, txtRegAddr = null!;

        public AuthForm()
        {
            this.Text = "Авторизация в VeloShop";
            this.Size = new Size(400, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            // Tab Вход
            var pageLogin = new TabPage("Вход");
            var lblL1 = new Label { Text = "Логин:", Location = new Point(30, 30), AutoSize = true };
            txtLogLogin = new TextBox { Location = new Point(30, 55), Size = new Size(320, 30) };

            var lblL2 = new Label { Text = "Пароль:", Location = new Point(30, 100), AutoSize = true };
            txtLogPass = new TextBox { PasswordChar = '*', Location = new Point(30, 125), Size = new Size(320, 30) };

            var btnDoLogin = new Button
            {
                Text = "Войти 🔓",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 150, 240),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(320, 45),
                Location = new Point(30, 180),
                Cursor = Cursors.Hand
            };
            btnDoLogin.FlatAppearance.BorderSize = 0;

            btnDoLogin.Click += (s, e) =>
            {
                var res = Database.Login(txtLogLogin.Text, txtLogPass.Text);
                if (res.Success)
                {
                    Database.CurrentUser = res.User;
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(res.Message, "Ошибка входа", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pageLogin.Controls.Add(lblL1);
            pageLogin.Controls.Add(txtLogLogin);
            pageLogin.Controls.Add(lblL2);
            pageLogin.Controls.Add(txtLogPass);
            pageLogin.Controls.Add(btnDoLogin);

            // Tab Регистрация
            var pageReg = new TabPage("Регистрация");
            txtRegLogin = new TextBox { PlaceholderText = "Логин (от 3 символов)", Location = new Point(30, 20), Size = new Size(320, 30) };
            txtRegPass = new TextBox { PlaceholderText = "Пароль (от 6 символов)", PasswordChar = '*', Location = new Point(30, 60), Size = new Size(320, 30) };
            txtRegName = new TextBox { PlaceholderText = "ФИО (Ваше имя)", Location = new Point(30, 100), Size = new Size(320, 30) };
            txtRegPhone = new TextBox { PlaceholderText = "Телефон (+7...)", Location = new Point(30, 140), Size = new Size(320, 30) };
            txtRegAddr = new TextBox { PlaceholderText = "Адрес доставки по умолчанию", Location = new Point(30, 180), Size = new Size(320, 30) };

            var btnDoReg = new Button
            {
                Text = "Зарегистрироваться 📝",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 160, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(320, 45),
                Location = new Point(30, 235),
                Cursor = Cursors.Hand
            };
            btnDoReg.FlatAppearance.BorderSize = 0;

            btnDoReg.Click += (s, e) =>
            {
                var res = Database.Register(txtRegLogin.Text, txtRegPass.Text, txtRegName.Text, txtRegPhone.Text, txtRegAddr.Text);
                if (res.Success)
                {
                    Database.CurrentUser = res.User;
                    MessageBox.Show(res.Message, "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(res.Message, "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pageReg.Controls.Add(txtRegLogin);
            pageReg.Controls.Add(txtRegPass);
            pageReg.Controls.Add(txtRegName);
            pageReg.Controls.Add(txtRegPhone);
            pageReg.Controls.Add(txtRegAddr);
            pageReg.Controls.Add(btnDoReg);

            tabs.TabPages.Add(pageLogin);
            tabs.TabPages.Add(pageReg);

            this.Controls.Add(tabs);
        }
    }
}
