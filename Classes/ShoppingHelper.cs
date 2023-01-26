using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShoppingHelper.Classes
{
    internal class ShoppingHelper
    {
        public MBHelper msgHndler = new();
        public static readonly HttpClient client = new();
        private string htmlData = "";
        readonly string surugaya = "www.suruga-ya.jp";
        public Form origin = Application.OpenForms["Form1"];
        public Item? currentItem;

        public async Task<bool> WebPageIsValid(string passedURL)
        {
            Debug.WriteLine("Task is running");
            bool responseRecieved = Uri.TryCreate(passedURL, UriKind.Absolute, out Uri myUri);
            if (responseRecieved)
            {
                try
                {
                    string responseBody = await client.GetStringAsync(passedURL);
                    Debug.WriteLine("Link {0} is valid", passedURL);
                    htmlData = responseBody;
                    return true;
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine("Exception caught\nMessage :{0} ", e.Message);
                    return false;
                }
            }
            string caption = "Failed to load data from URL";
            string message = $"Please check that {passedURL} has been entered correctly";
            msgHndler.ErrorMB(caption, message);
            return false;
        }

        public void DisplayProduct(Item item)
        {
            origin = Application.OpenForms["Form1"];
            var panel1 = origin.Controls["productPanel"];
            PictureBox imageHolder = (PictureBox)panel1.Controls["productImage"];
            Label priceHolder = (Label)panel1.Controls["priceTag"];
            priceHolder.Text = item.price.ToString("c");
            if (item.price == 0)
            {
                priceHolder.Text = "Item is sold out";
            }
            Debug.WriteLine($"Price retrieved: {item.price}");
            var infoHolder = panel1.Controls["productInfo"];
            string productInfo = item.Name;
            infoHolder.Text = productInfo;
            string imageLink = item.image;
            Debug.WriteLine(imageLink);
            imageHolder.Load(imageLink);
        }

        public async Task<bool> ProductRetrievedFromURI(string passedURL)
        {
            Debug.WriteLine($"Retrieving data from {passedURL}");
            var task = await (WebPageIsValid(passedURL));
            if (task)
            {
                Debug.WriteLine(SiteRequestor(passedURL));
                if (string.Compare(SiteRequestor(passedURL), surugaya) == 0)
                {
                    try
                    {
                        Item item = SurugayaGrab(htmlData, passedURL);
                        DisplayProduct(item);
                        currentItem = item;
                        return true;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
            }
            return false;
        }

        private Item SurugayaGrab(string passedBody, string passedURL)
        {

            string link = passedURL;
            Encoding utf8 = new UTF8Encoding(true);
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ja-JP");
            string htmlCode = passedBody;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlCode);
            string cost = ItemIsAvaliable(doc);

            Byte[] eB = utf8.GetBytes(doc.DocumentNode.SelectSingleNode("//h1[@id='item_title']").InnerText);
            string product = doc.DocumentNode.SelectSingleNode("//h1[@id='item_title']").InnerText;
            string value = "";
            string soldOut = "Item is Sold Out";
            int price = 0;
            if (string.Compare(cost, soldOut) != 0)
            {
                for (int i = 0; i < cost.Length; i++)
                {
                    if (Char.IsNumber(cost[i]))
                    {
                        value += cost[i];
                    }
                }
                price = Int32.Parse(value);
            }
            else
            {
                value = soldOut;
            }

            string imageHolder = doc.DocumentNode
                      .SelectNodes("//img[@class='img-fluid main-pro-img']").First()
                        .Attributes["src"].Value;
            string cleanText = String.Concat(product.Where(c => !Char.IsWhiteSpace(c)));
            string unicodeString = cleanText;
            Console.WriteLine(cleanText);
            Debug.WriteLine($"Cost: {value}");



            string vendor = "Surugaya";
            Item newProduct = new(price, cleanText, vendor, link, imageHolder);
            return newProduct;
        }

        private string ItemIsAvaliable(HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            var doc = htmlDoc;
            string value = "Item is Sold Out";
            var priceNode = doc.DocumentNode
                     .SelectSingleNode("//span[@class='text-price-detail price-buy']");
            if (priceNode != null)
            {
                value = doc.DocumentNode
                     .SelectSingleNode("//span[@class='text-price-detail price-buy']").InnerText;

            }
            return value;
        }

        private static string SiteRequestor(string query)
        {
            string begin = @"s:{1}\/{2}";
            string end = @"\.jp{1}";
            Match idx1 = Regex.Match(query, begin);
            string trim1 = query.Substring(8, (query.Length - (idx1.Index + 8)));
            Match idx3 = Regex.Match(trim1, end);
            //const int StartIndex = 0;
            string trim2 = trim1[..(idx3.Index + 3)];
            return trim2;
        }


    }
    public class ItemHelper : SQLHandler
    {   

        private string CreateWishlist(string a)
        {
            string query = $"INSERT INTO [WishList] ([productString]) VALUES ({a})";
            return query;
        }

        private string InsertQueryItem(Item item)
        {
            string query = $"INSERT INTO [Item] ([Name], [Price], [Vendor], [Link], [Image]) " +
                $"VALUES (@name, @price, @vendor, @link, @image)";
            return query;
        }

        private string InsertQueryWL()
        {
            string query = $"INSERT INTO [WishList] ([productString]) VALUES (@products)";
            return query;
        }

        private string UpdateQueryWL()
        {
            string query = "UPDATE WishList SET productString = @productString" +
                " WHERE id = @id";
            return query;
        }

        public void AddItemToDB(Item item)
        {
            string queryString = InsertQueryItem(item); 
            try
            {
                using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
                {

                    Debug.WriteLine(connection.Database);
                    Debug.WriteLine(queryString);
                    SqliteCommand command = new SqliteCommand(
                    queryString, connection);
                    command.Parameters.AddWithValue("@name", item.Name);
                    command.Parameters.AddWithValue("@price", item.price);
                    command.Parameters.AddWithValue("@vendor", item.Vendor);
                    command.Parameters.AddWithValue("@link", item.link);
                    command.Parameters.AddWithValue("@image", item.image);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MBHelper mB = new MBHelper();
                string caption = $"Error Adding to DB";
                mB.ErrorMB(ex.Message, caption);
            }
        }

        public void CreateWishlistOne()
        {
            string queryString = CreateWishlist("abc");
            try
            {
                using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
                {

                    Debug.WriteLine(connection.Database);
                    Debug.WriteLine(queryString);
                    SqliteCommand command = new SqliteCommand(
                    queryString, connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MBHelper mB = new MBHelper();
                string caption = $"Error Adding to DB";
                mB.ErrorMB(ex.Message, caption);
            }
        }

        private void UpdateWishListData(string str)
        {
            string queryString = UpdateQueryWL();
            try
            {
                using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
                {

                    Debug.WriteLine(connection.Database);
                    Debug.WriteLine(queryString);
                    SqliteCommand command = new SqliteCommand(
                    queryString, connection);
                    command.Parameters.Add("@productString", SqliteType.Text);
                    command.Parameters.Add("@id", SqliteType.Integer);
                    command.Parameters["@productString"].Value = str;
                    command.Parameters["@id"].Value = 1;
                    Debug.WriteLine($"Product String " + str);    
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MBHelper mB = new MBHelper();
                string caption = $"Error Adding to DB";
                mB.ErrorMB(ex.Message, caption);
            }
        }

        public async Task<bool> AddedToWishList(Item item)
        {   
            AddItemToDB(item);
            string queryString = UpdateQueryWL();
            string caption;
            string message;
            Item existingItem = QueriedItem(item.Name);
            WishList list = WishListData();
            if(list == null)
            {
                Debug.WriteLine("Error list is null");
            }
            if(list != null)
            {
                string productString = list.productString;
                Debug.WriteLine($"Current ps: {productString}");
                if (item != null)
                {
                    string name = item.Name;
                    if (productString.Contains(name))
                    {
                        caption = $"Cannot add {name} to wishlist";
                        message = $"Add Error: {name} is already in wishlist.";
                        MBHelper mB = new();
                        mB.ErrorMB(message, caption);
                        return false;
                    }
                    if (!productString.Contains(name))
                    {   
                        productString += $"{name},";  
                        UpdateWishListData(productString);
                        await Task.Delay(1);
                        return true;
                    }
                }
            }
            return false;
        
            
        }

        public List<Item> Products()
        {
            List<Item> list = new();
            string queryString =
           $"SELECT * FROM Item;";
            using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
            {
                Debug.WriteLine(queryString);
                SqliteCommand command = new SqliteCommand(
                queryString, connection);
                connection.Open();
                SqliteDataReader reader;
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    WishList((IDataRecord)reader, list);
                }
                connection.Close();
                return list;
            }
        }

        public WishList WishListData()
        {
           
            string queryString =
            $"SELECT * FROM WishList;";
            using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
            {
                WishList list = new("");           
                Debug.WriteLine(queryString);
                SqliteCommand command = new SqliteCommand(
                queryString, connection);
                connection.Open();
                SqliteDataReader reader;
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    list = ReturnedWishList((IDataRecord)reader);
                }
                connection.Close();
                return list;
            }
        }

        public Item QueriedItem(string name)
        {
            Item? product = null; 
            int index = 0;
            string queryString =
           $"SELECT * FROM Item WHERE Name='{name}';";
            try
            {
                using (SqliteConnection connection = new SqliteConnection(
                        GetConnectionString()))
                {

                    Debug.WriteLine(connection.Database);
                    Debug.WriteLine(queryString);
                    SqliteCommand command = new SqliteCommand(
                    queryString, connection);
                    connection.Open();
                    SqliteDataReader reader;
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        product = ReturnedProduct((IDataRecord)reader);
                        Debug.WriteLine($"Ran {index} times");
                        index++;
                    }
                    connection.Close();
                    return product;

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");

            }
            return product;
        }

        private Item? ReturnedProduct(IDataRecord dataRecord)
        {
            Item? item = null;
            if (dataRecord[1] is not null && dataRecord[2] != null && dataRecord[3] != null
                && dataRecord[4] != null && dataRecord[5] != null)
            {
                string name = (string)dataRecord[1];
                int price = (int)(long)dataRecord[2];
                string vendor = (string)dataRecord[3];
                string image = (string)dataRecord[5];
                string link = (string)dataRecord[4];

                item = new(price, name, vendor, link, image);
                return item;
            }

            return item;    

        }

        private void WishList(IDataRecord dataRecord, List<Item> data)
        {
            if (dataRecord[1] is not null && dataRecord[2] != null && dataRecord[3] != null
                 && dataRecord[4] != null && dataRecord[5] != null)
            {
                string name = (string)dataRecord[1];
                int price = (int)(long)dataRecord[2];
                string vendor = (string)dataRecord[3];
                string image = (string)dataRecord[5];
                string link = (string)dataRecord[4];
                
                Item item = new(price, name, vendor, link, image);
                data.Add(item);
            }
              

        }

        private WishList ReturnedWishList(IDataRecord dataRecord)
        {
            WishList item = null;         
            if (dataRecord[1] != null)
            {
                string name = (string)dataRecord[1];
                Debug.WriteLine(name);
                if(name.Length < 0)
                {
                    name = " ";
                }
                Debug.WriteLine(name);
                WishList list = new(name);
                return list;
            }

            return item;

        }
    }


    public class WishList
    {
        public string productString;
        public WishList(string productString)
        {
            this.productString = productString; 
        }
    }

    public class Item
    {

        public int price;
        public string Name;
        public string Vendor;
        public string link;
        public string image;
        public string comment = "";

        public Item(int price, string Name, string Vendor, string link, string image)
        {
            this.Name = Name;
            this.Vendor = Vendor;
            this.price = price;
            this.link = link;
            this.image = image;
        }

        public void SetPrice(int price)
        {
            this.price = price;
        }

        public void AddComment(string comment)
        {
            this.comment = comment;
        }
    }

    public class ItemComparer : IComparer
    {
        public int Column { get; set; }

        public SortOrder Order { get; set; }
        private CaseInsensitiveComparer ObjectCompare;

        public ItemComparer()
        {
            Order = SortOrder.None;
            ObjectCompare = new CaseInsensitiveComparer();
        }
        public int Compare(object x, object y)
        {
            int result;
            ListViewItem listviewX, listviewY;

            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;
            result = ObjectCompare.Compare(listviewX.SubItems[Column].Text, listviewY.SubItems[Column].Text);
            string xStr = listviewX.SubItems[Column].Text;
            string yStr = listviewY.SubItems[Column].Text;
            int xValue = 0;
            int yValue = 0;
            int.TryParse(xStr, System.Globalization.NumberStyles.Currency, null, out xValue);
            int.TryParse(yStr, System.Globalization.NumberStyles.Currency, null, out yValue);
            if (yValue != 0 && xValue != 0)
            {
                Debug.WriteLine($"Number, comparing: {xValue} to {yValue}");
                result = xValue - yValue;
            }

            if (Order == SortOrder.Descending)
            {
                return (-result);
            }
            else if (Order == SortOrder.Ascending)
            {
                return result;
            }
            return 0;
        }
        public int SortColumn
        {
            set
            {
                Column = value;
            }
            get
            {
                return Column;
            }
        }

        public SortOrder OrderCol
        {
            set
            {
                Order = value;
            }
            get
            {
                return Order;
            }
        }
    }

}
