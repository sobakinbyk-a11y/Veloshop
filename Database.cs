using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace VeloShop
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string AvatarPath { get; set; } = "";
        public string Role { get; set; } = "User"; // "User" или "Admin"
        public bool IsBanned { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double Price { get; set; }
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CartItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public double Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public string ImagePath { get; set; } = "";
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserLogin { get; set; } = "";
        public string OrderDate { get; set; } = "";
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Новый";
        public string DeliveryAddress { get; set; } = "";
        public string ContactPhone { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public double PriceAtPurchase { get; set; }
    }

    public static class Database
    {
        private static readonly string DbFileName = "veloshop.db";
        private static readonly string ConnectionString = $"Data Source={DbFileName}";

        public static User? CurrentUser { get; set; } = null;

        public static void Init()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT,
                    Phone TEXT,
                    Address TEXT,
                    AvatarPath TEXT,
                    Role TEXT NOT NULL DEFAULT 'User',
                    IsBanned INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Description TEXT,
                    ImagePath TEXT,
                    Stock INTEGER NOT NULL DEFAULT 10,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS CartItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL DEFAULT 1,
                    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Favorites (
                    UserId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    PRIMARY KEY(UserId, ProductId),
                    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    OrderDate TEXT NOT NULL,
                    TotalPrice REAL NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Новый',
                    DeliveryAddress TEXT NOT NULL,
                    ContactPhone TEXT NOT NULL,
                    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS OrderItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    PriceAtPurchase REAL NOT NULL,
                    FOREIGN KEY(OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();

            // Создание админа по умолчанию (admin / admin123)
            var checkAdminCmd = connection.CreateCommand();
            checkAdminCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = 'admin'";
            long adminCount = (long)(checkAdminCmd.ExecuteScalar() ?? 0L);
            if (adminCount == 0)
            {
                var insertAdmin = connection.CreateCommand();
                insertAdmin.CommandText = @"
                    INSERT INTO Users (Login, PasswordHash, FullName, Phone, Address, Role, CreatedAt)
                    VALUES ('admin', @hash, 'Главный Администратор', '+7 (999) 000-00-00', 'Главный офис VeloShop', 'Admin', @date);
                ";
                insertAdmin.Parameters.AddWithValue("@hash", HashPassword("admin123"));
                insertAdmin.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                insertAdmin.ExecuteNonQuery();
            }

            // Наполнение первичными товарами, если каталог пуст
            var checkProdCmd = connection.CreateCommand();
            checkProdCmd.CommandText = "SELECT COUNT(*) FROM Products";
            long prodCount = (long)(checkProdCmd.ExecuteScalar() ?? 0L);
            if (prodCount == 0)
            {
                SeedProducts(connection);
            }
        }

        private static void SeedProducts(SqliteConnection conn)
        {
            var seedCmd = conn.CreateCommand();
            seedCmd.CommandText = @"
                INSERT INTO Products (Name, Category, Price, Description, ImagePath, Stock, IsActive) VALUES
                ('Forward Sporting 29 X', 'Горные', 28990, 'Надежный горный хардтейл с колесами 29 дюймов, дисковыми тормозами и трансмиссией Shimano.', '', 8, 1),
                ('Format 5222 Gravel', 'Шоссейные', 68500, 'Универсальный гравийный велосипед с прочной хромомолибденовой рамой и широкими покрышками.', '', 5, 1),
                ('Outleap Track Pro', 'Трековые', 34200, 'Легкий алюминиевый синглспид/фикс для города и трека с агрессивной геометрией.', '', 4, 1),
                ('Shulz Krabi City', 'Городские', 37900, 'Комфортный складной городской велосипед с планетарной втулкой Shimano Nexus 3.', '', 6, 1),
                ('Шлем VeloGuard Aero', 'Аксессуары', 4500, 'Аэродинамический легкий шлем с вентиляционными каналами и регулировкой посадки.', '', 20, 1),
                ('Фонарь Cosmic Light 1000LM', 'Аксессуары', 2800, 'Яркий влагозащищенный аккумуляторный велофонарь с USB-зарядкой.', '', 15, 1);
            ";
            seedCmd.ExecuteNonQuery();
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "_veloshop_salt_2026"));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static (bool Success, string Message, User? User) Register(string login, string password, string fullName, string phone, string address)
        {
            if (string.IsNullOrWhiteSpace(login) || login.Length < 3 || login.Length > 20)
                return (false, "Логин должен быть от 3 до 20 символов!", null);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Пароль должен содержать не менее 6 символов!", null);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = @login";
            checkCmd.Parameters.AddWithValue("@login", login.Trim());
            if ((long)(checkCmd.ExecuteScalar() ?? 0L) > 0)
                return (false, "Пользователь с таким логином уже существует!", null);

            var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Users (Login, PasswordHash, FullName, Phone, Address, Role, CreatedAt)
                VALUES (@login, @hash, @name, @phone, @address, 'User', @date);
                SELECT last_insert_rowid();
            ";
            insertCmd.Parameters.AddWithValue("@login", login.Trim());
            insertCmd.Parameters.AddWithValue("@hash", HashPassword(password));
            insertCmd.Parameters.AddWithValue("@name", fullName.Trim());
            insertCmd.Parameters.AddWithValue("@phone", phone.Trim());
            insertCmd.Parameters.AddWithValue("@address", address.Trim());
            insertCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            long newId = (long)insertCmd.ExecuteScalar()!;
            var newUser = new User
            {
                Id = (int)newId,
                Login = login.Trim(),
                FullName = fullName.Trim(),
                Phone = phone.Trim(),
                Address = address.Trim(),
                Role = "User",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return (true, "Регистрация успешно завершена!", newUser);
        }

        public static (bool Success, string Message, User? User) Login(string login, string password)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Login, FullName, Phone, Address, AvatarPath, Role, IsBanned, CreatedAt 
                FROM Users 
                WHERE Login = @login AND PasswordHash = @hash
            ";
            cmd.Parameters.AddWithValue("@login", login.Trim());
            cmd.Parameters.AddWithValue("@hash", HashPassword(password));

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var user = new User
                {
                    Id = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Address = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    AvatarPath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Role = reader.GetString(6),
                    IsBanned = reader.GetInt32(7) == 1,
                    CreatedAt = reader.GetString(8)
                };

                if (user.IsBanned)
                    return (false, "Ваш аккаунт заблокирован администратором за нарушение правил!", null);

                return (true, "Успешный вход!", user);
            }

            return (false, "Неверный логин или пароль!", null);
        }

        public static bool UpdateProfile(int userId, string fullName, string phone, string address, string avatarPath)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users 
                SET FullName = @name, Phone = @phone, Address = @addr, AvatarPath = @avatar
                WHERE Id = @id
            ";
            cmd.Parameters.AddWithValue("@name", fullName.Trim());
            cmd.Parameters.AddWithValue("@phone", phone.Trim());
            cmd.Parameters.AddWithValue("@addr", address.Trim());
            cmd.Parameters.AddWithValue("@avatar", avatarPath);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool ChangePassword(int userId, string oldPass, string newPass)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Id = @id AND PasswordHash = @hash";
            checkCmd.Parameters.AddWithValue("@id", userId);
            checkCmd.Parameters.AddWithValue("@hash", HashPassword(oldPass));
            if ((long)(checkCmd.ExecuteScalar() ?? 0L) == 0)
                return false;

            var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = "UPDATE Users SET PasswordHash = @hash WHERE Id = @id";
            updateCmd.Parameters.AddWithValue("@hash", HashPassword(newPass));
            updateCmd.Parameters.AddWithValue("@id", userId);
            return updateCmd.ExecuteNonQuery() > 0;
        }

        public static List<Product> GetProducts(string search = "", string category = "Все", double minPrice = 0, double maxPrice = double.MaxValue, bool onlyFavorites = false, int userId = 0)
        {
            var list = new List<Product>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var sql = new StringBuilder("SELECT p.Id, p.Name, p.Category, p.Price, p.Description, p.ImagePath, p.Stock, p.IsActive FROM Products p ");

            if (onlyFavorites && userId > 0)
            {
                sql.Append("INNER JOIN Favorites f ON p.Id = f.ProductId AND f.UserId = @userId ");
            }

            sql.Append("WHERE p.IsActive = 1 AND p.Price >= @minPrice ");

            if (maxPrice < double.MaxValue)
                sql.Append("AND p.Price <= @maxPrice ");

            if (category != "Все")
                sql.Append("AND p.Category = @cat ");

            if (!string.IsNullOrWhiteSpace(search))
                sql.Append("AND (p.Name LIKE @search OR p.Description LIKE @search) ");

            sql.Append("ORDER BY p.Id DESC");

            var cmd = conn.CreateCommand();
            cmd.CommandText = sql.ToString();
            cmd.Parameters.AddWithValue("@minPrice", minPrice);
            if (maxPrice < double.MaxValue) cmd.Parameters.AddWithValue("@maxPrice", maxPrice);
            if (category != "Все") cmd.Parameters.AddWithValue("@cat", category);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
            if (onlyFavorites && userId > 0) cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2),
                    Price = reader.GetDouble(3),
                    Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ImagePath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Stock = reader.GetInt32(6),
                    IsActive = reader.GetInt32(7) == 1
                });
            }
            return list;
        }

        public static bool ToggleFavorite(int userId, int productId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Favorites WHERE UserId = @uid AND ProductId = @pid";
            checkCmd.Parameters.AddWithValue("@uid", userId);
            checkCmd.Parameters.AddWithValue("@pid", productId);
            long exists = (long)(checkCmd.ExecuteScalar() ?? 0L);

            var cmd = conn.CreateCommand();
            if (exists > 0)
            {
                cmd.CommandText = "DELETE FROM Favorites WHERE UserId = @uid AND ProductId = @pid";
            }
            else
            {
                cmd.CommandText = "INSERT INTO Favorites (UserId, ProductId) VALUES (@uid, @pid)";
            }
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.ExecuteNonQuery();

            return exists == 0; // Возвращает true, если добавлен
        }

        public static bool IsFavorite(int userId, int productId)
        {
            if (userId <= 0) return false;
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Favorites WHERE UserId = @uid AND ProductId = @pid";
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@pid", productId);
            return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        }

        public static bool AddToCart(int userId, int productId, int qty = 1)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT Id, Quantity FROM CartItems WHERE UserId = @uid AND ProductId = @pid";
            checkCmd.Parameters.AddWithValue("@uid", userId);
            checkCmd.Parameters.AddWithValue("@pid", productId);

            using var reader = checkCmd.ExecuteReader();
            if (reader.Read())
            {
                int cartId = reader.GetInt32(0);
                int currentQty = reader.GetInt32(1);
                reader.Close();

                var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE CartItems SET Quantity = @qty WHERE Id = @id";
                updateCmd.Parameters.AddWithValue("@qty", currentQty + qty);
                updateCmd.Parameters.AddWithValue("@id", cartId);
                return updateCmd.ExecuteNonQuery() > 0;
            }
            else
            {
                reader.Close();
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO CartItems (UserId, ProductId, Quantity) VALUES (@uid, @pid, @qty)";
                insertCmd.Parameters.AddWithValue("@uid", userId);
                insertCmd.Parameters.AddWithValue("@pid", productId);
                insertCmd.Parameters.AddWithValue("@qty", qty);
                return insertCmd.ExecuteNonQuery() > 0;
            }
        }

        public static List<CartItem> GetCart(int userId)
        {
            var list = new List<CartItem>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.Id, c.UserId, c.ProductId, p.Name, p.Price, c.Quantity, p.Stock, p.ImagePath
                FROM CartItems c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE c.UserId = @uid
            ";
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CartItem
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    ProductId = reader.GetInt32(2),
                    ProductName = reader.GetString(3),
                    Price = reader.GetDouble(4),
                    Quantity = reader.GetInt32(5),
                    Stock = reader.GetInt32(6),
                    ImagePath = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }
            return list;
        }

        public static void UpdateCartQty(int cartItemId, int newQty)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            if (newQty <= 0)
            {
                var delCmd = conn.CreateCommand();
                delCmd.CommandText = "DELETE FROM CartItems WHERE Id = @id";
                delCmd.Parameters.AddWithValue("@id", cartItemId);
                delCmd.ExecuteNonQuery();
            }
            else
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE CartItems SET Quantity = @qty WHERE Id = @id";
                cmd.Parameters.AddWithValue("@qty", newQty);
                cmd.Parameters.AddWithValue("@id", cartItemId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void ClearCart(int userId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM CartItems WHERE UserId = @uid";
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.ExecuteNonQuery();
        }

        public static (bool Success, string Message) Checkout(int userId, string address, string phone)
        {
            var cart = GetCart(userId);
            if (cart.Count == 0)
                return (false, "Ваша корзина пуста!");

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                double total = 0;
                foreach (var item in cart)
                {
                    if (item.Quantity > item.Stock)
                    {
                        transaction.Rollback();
                        return (false, $"Недостаточно товара '{item.ProductName}' на складе (остаток: {item.Stock})!");
                    }
                    total += item.Price * item.Quantity;
                }

                // Создаем заказ
                var orderCmd = conn.CreateCommand();
                orderCmd.Transaction = transaction;
                orderCmd.CommandText = @"
                    INSERT INTO Orders (UserId, OrderDate, TotalPrice, Status, DeliveryAddress, ContactPhone)
                    VALUES (@uid, @date, @total, 'Новый', @addr, @phone);
                    SELECT last_insert_rowid();
                ";
                orderCmd.Parameters.AddWithValue("@uid", userId);
                orderCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                orderCmd.Parameters.AddWithValue("@total", total);
                orderCmd.Parameters.AddWithValue("@addr", address);
                orderCmd.Parameters.AddWithValue("@phone", phone);

                long orderId = (long)orderCmd.ExecuteScalar()!;

                // Создаем элементы заказа и урезаем склад
                foreach (var item in cart)
                {
                    var itemCmd = conn.CreateCommand();
                    itemCmd.Transaction = transaction;
                    itemCmd.CommandText = @"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, PriceAtPurchase)
                        VALUES (@oid, @pid, @qty, @price);
                        UPDATE Products SET Stock = Stock - @qty WHERE Id = @pid;
                    ";
                    itemCmd.Parameters.AddWithValue("@oid", orderId);
                    itemCmd.Parameters.AddWithValue("@pid", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.ExecuteNonQuery();
                }

                // Чистим корзину
                var clearCmd = conn.CreateCommand();
                clearCmd.Transaction = transaction;
                clearCmd.CommandText = "DELETE FROM CartItems WHERE UserId = @uid";
                clearCmd.Parameters.AddWithValue("@uid", userId);
                clearCmd.ExecuteNonQuery();

                transaction.Commit();
                return (true, $"Заказ №{orderId} успешно оформлен на сумму {total:N0} ₽!");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return (false, $"Ошибка оформления заказа: {ex.Message}");
            }
        }

        public static List<Order> GetOrders(int userId = 0)
        {
            var orders = new List<Order>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var sql = "SELECT o.Id, o.UserId, u.Login, o.OrderDate, o.TotalPrice, o.Status, o.DeliveryAddress, o.ContactPhone FROM Orders o INNER JOIN Users u ON o.UserId = u.Id ";
            if (userId > 0)
                sql += "WHERE o.UserId = @uid ";
            sql += "ORDER BY o.Id DESC";

            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (userId > 0) cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    UserLogin = reader.GetString(2),
                    OrderDate = reader.GetString(3),
                    TotalPrice = reader.GetDouble(4),
                    Status = reader.GetString(5),
                    DeliveryAddress = reader.GetString(6),
                    ContactPhone = reader.GetString(7)
                });
            }
            reader.Close();

            // Загружаем состав заказов
            foreach (var o in orders)
            {
                var itemCmd = conn.CreateCommand();
                itemCmd.CommandText = @"
                    SELECT oi.Id, oi.OrderId, oi.ProductId, p.Name, oi.Quantity, oi.PriceAtPurchase
                    FROM OrderItems oi
                    LEFT JOIN Products p ON oi.ProductId = p.Id
                    WHERE oi.OrderId = @oid
                ";
                itemCmd.Parameters.AddWithValue("@oid", o.Id);
                using var itemReader = itemCmd.ExecuteReader();
                while (itemReader.Read())
                {
                    o.Items.Add(new OrderItem
                    {
                        Id = itemReader.GetInt32(0),
                        OrderId = itemReader.GetInt32(1),
                        ProductId = itemReader.GetInt32(2),
                        ProductName = itemReader.IsDBNull(3) ? "Товар удален" : itemReader.GetString(3),
                        Quantity = itemReader.GetInt32(4),
                        PriceAtPurchase = itemReader.GetDouble(5)
                    });
                }
            }

            return orders;
        }

        public static void UpdateOrderStatus(int orderId, string status)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Orders SET Status = @status WHERE Id = @id";
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", orderId);
            cmd.ExecuteNonQuery();
        }

        // Администрирование товаров
        public static void SaveProduct(Product p)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            if (p.Id == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Products (Name, Category, Price, Description, ImagePath, Stock, IsActive)
                    VALUES (@name, @cat, @price, @desc, @img, @stock, 1)
                ";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE Products 
                    SET Name = @name, Category = @cat, Price = @price, Description = @desc, ImagePath = @img, Stock = @stock, IsActive = @active
                    WHERE Id = @id
                ";
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.Parameters.AddWithValue("@active", p.IsActive ? 1 : 0);
            }
            cmd.Parameters.AddWithValue("@name", p.Name.Trim());
            cmd.Parameters.AddWithValue("@cat", p.Category);
            cmd.Parameters.AddWithValue("@price", p.Price);
            cmd.Parameters.AddWithValue("@desc", p.Description.Trim());
            cmd.Parameters.AddWithValue("@img", p.ImagePath);
            cmd.Parameters.AddWithValue("@stock", p.Stock);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteProduct(int productId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", productId);
            cmd.ExecuteNonQuery();
        }

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Login, FullName, Phone, Address, AvatarPath, Role, IsBanned, CreatedAt FROM Users ORDER BY Id DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Address = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    AvatarPath = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Role = reader.GetString(6),
                    IsBanned = reader.GetInt32(7) == 1,
                    CreatedAt = reader.GetString(8)
                });
            }
            return list;
        }

        public static void ToggleBanUser(int userId, bool ban)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsBanned = @ban WHERE Id = @id";
            cmd.Parameters.AddWithValue("@ban", ban ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }
    }
}
